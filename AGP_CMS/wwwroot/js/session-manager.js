/**
 * AGP_CMS Session Manager
 * Handles authentication state management using HTTP-only cookies
 * Replaces the previous timer-based session approach
 */

(function() {
    'use strict';

    const SessionManager = {
        // Configuration
        config: {
            tokenCookieName: 'ASHATCore_AuthToken',
            userCookieName: 'ASHATCore_User',
            sessionCheckInterval: 60000, // Check session every 60 seconds
            apiBaseUrl: '/api'
        },

        // Initialize session manager
        init: function() {
            this.checkSession();
            this.setupSessionMonitoring();
            this.updateUIElements();
        },

        // Check if user is authenticated
        isAuthenticated: function() {
            // Check for auth token cookie
            const token = this.getCookie(this.config.tokenCookieName);
            return token !== null && token !== '';
        },

        // Get cookie value by name
        getCookie: function(name) {
            const nameEQ = name + "=";
            const ca = document.cookie.split(';');
            for(let i = 0; i < ca.length; i++) {
                let c = ca[i];
                while (c.charAt(0) === ' ') c = c.substring(1, c.length);
                if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length, c.length);
            }
            return null;
        },

        // Set cookie
        setCookie: function(name, value, days) {
            let expires = "";
            if (days) {
                const date = new Date();
                date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
                expires = "; expires=" + date.toUTCString();
            }
            document.cookie = name + "=" + (value || "") + expires + "; path=/; SameSite=Lax";
        },

        // Delete cookie
        deleteCookie: function(name) {
            document.cookie = name + '=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT; SameSite=Lax';
        },

        // Get current user information
        getCurrentUser: function() {
            const userJson = this.getCookie(this.config.userCookieName);
            if (userJson) {
                try {
                    return JSON.parse(decodeURIComponent(userJson));
                } catch (e) {
                    console.error('Failed to parse user cookie:', e);
                    return null;
                }
            }
            return null;
        },

        // Check session status with server
        checkSession: async function() {
            if (!this.isAuthenticated()) {
                this.handleUnauthenticated();
                return false;
            }

            try {
                const token = this.getCookie(this.config.tokenCookieName);
                const response = await fetch(this.config.apiBaseUrl + '/auth/verify', {
                    method: 'GET',
                    headers: {
                        'Authorization': 'Bearer ' + token,
                        'Content-Type': 'application/json'
                    },
                    credentials: 'include'
                });

                if (response.ok) {
                    const data = await response.json();
                    if (data.valid) {
                        this.handleAuthenticated(data.user);
                        return true;
                    }
                }
                
                // Session invalid
                this.handleSessionExpired();
                return false;
            } catch (error) {
                console.error('Session check failed:', error);
                // Don't log out on network errors, keep existing session
                return this.isAuthenticated();
            }
        },

        // Setup periodic session monitoring
        setupSessionMonitoring: function() {
            // Check session periodically
            setInterval(() => {
                this.checkSession();
            }, this.config.sessionCheckInterval);

            // Check on page visibility change
            document.addEventListener('visibilitychange', () => {
                if (!document.hidden) {
                    this.checkSession();
                }
            });

            // Sync auth state across tabs
            window.addEventListener('storage', (e) => {
                if (e.key === this.config.tokenCookieName || e.key === 'logout_event') {
                    this.checkSession();
                    this.updateUIElements();
                }
            });
        },

        // Handle authenticated state
        handleAuthenticated: function(user) {
            this.updateUIElements(user);
            this.dispatchAuthEvent('authenticated', user);
        },

        // Handle unauthenticated state
        handleUnauthenticated: function() {
            this.updateUIElements(null);
            this.dispatchAuthEvent('unauthenticated');
        },

        // Handle session expired
        handleSessionExpired: function() {
            this.clearAuthData();
            this.updateUIElements(null);
            this.dispatchAuthEvent('session_expired');
            
            // Show session expired message if not on login page
            if (!window.location.pathname.includes('/login')) {
                this.showSessionExpiredMessage();
            }
        },

        // Clear authentication data
        clearAuthData: function() {
            this.deleteCookie(this.config.tokenCookieName);
            this.deleteCookie(this.config.userCookieName);
            localStorage.removeItem('ASHATCore_token'); // Legacy support
            
            // Notify other tabs
            localStorage.setItem('logout_event', Date.now().toString());
        },

        // Update UI elements based on auth state
        updateUIElements: function(user) {
            const isAuth = user !== null || this.isAuthenticated();
            user = user || this.getCurrentUser();

            // Update user display elements
            const userDisplays = document.querySelectorAll('[data-auth-user-display]');
            userDisplays.forEach(el => {
                if (isAuth && user) {
                    el.textContent = user.username || user.email || 'User';
                    el.style.display = '';
                } else {
                    el.style.display = 'none';
                }
            });

            // Update authenticated-only elements
            const authElements = document.querySelectorAll('[data-auth-required]');
            authElements.forEach(el => {
                el.style.display = isAuth ? '' : 'none';
            });

            // Update guest-only elements
            const guestElements = document.querySelectorAll('[data-guest-only]');
            guestElements.forEach(el => {
                el.style.display = isAuth ? 'none' : '';
            });

            // Update login/logout buttons
            const loginBtns = document.querySelectorAll('[data-auth-login]');
            loginBtns.forEach(btn => {
                btn.style.display = isAuth ? 'none' : '';
            });

            const logoutBtns = document.querySelectorAll('[data-auth-logout]');
            logoutBtns.forEach(btn => {
                btn.style.display = isAuth ? '' : 'none';
                if (isAuth) {
                    btn.addEventListener('click', (e) => {
                        e.preventDefault();
                        this.logout();
                    });
                }
            });
        },

        // Show session expired message
        showSessionExpiredMessage: function() {
            const message = document.createElement('div');
            message.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                background: rgba(220, 38, 38, 0.95);
                color: white;
                padding: 15px 20px;
                border-radius: 8px;
                box-shadow: 0 4px 12px rgba(0,0,0,0.3);
                z-index: 10000;
                font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
                animation: slideIn 0.3s ease-out;
            `;
            message.innerHTML = `
                <strong>Session Expired</strong>
                <p style="margin: 5px 0 0 0; font-size: 0.9em;">Please log in again to continue.</p>
            `;

            document.body.appendChild(message);

            // Add animation
            const style = document.createElement('style');
            style.textContent = '@keyframes slideIn { from { transform: translateX(400px); opacity: 0; } to { transform: translateX(0); opacity: 1; } }';
            document.head.appendChild(style);

            // Auto-remove after 5 seconds
            setTimeout(() => {
                message.style.animation = 'slideIn 0.3s ease-out reverse';
                setTimeout(() => message.remove(), 300);
            }, 5000);
        },

        // Logout
        logout: async function() {
            try {
                const token = this.getCookie(this.config.tokenCookieName);
                await fetch(this.config.apiBaseUrl + '/auth/logout', {
                    method: 'POST',
                    headers: {
                        'Authorization': 'Bearer ' + token,
                        'Content-Type': 'application/json'
                    },
                    credentials: 'include'
                });
            } catch (error) {
                console.error('Logout request failed:', error);
            } finally {
                this.clearAuthData();
                this.dispatchAuthEvent('logout');
                window.location.href = '/login';
            }
        },

        // Dispatch custom authentication events
        dispatchAuthEvent: function(eventType, detail) {
            const event = new CustomEvent('cms:auth:' + eventType, {
                detail: detail,
                bubbles: true
            });
            window.dispatchEvent(event);
        },

        // Get auth token for API calls
        getAuthToken: function() {
            return this.getCookie(this.config.tokenCookieName);
        },

        // Create authorized fetch wrapper
        authorizedFetch: async function(url, options = {}) {
            const token = this.getAuthToken();
            if (!token) {
                throw new Error('Not authenticated');
            }

            options.headers = options.headers || {};
            options.headers['Authorization'] = 'Bearer ' + token;
            options.credentials = 'include';

            const response = await fetch(url, options);

            // Handle unauthorized response
            if (response.status === 401) {
                this.handleSessionExpired();
                throw new Error('Authentication required');
            }

            return response;
        }
    };

    // Export to window for global access
    window.SessionManager = SessionManager;

    // Auto-initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => SessionManager.init());
    } else {
        SessionManager.init();
    }

    // Expose authentication events for developers
    // Usage: window.addEventListener('cms:auth:authenticated', (e) => console.log(e.detail));
    console.log('AGP_CMS Session Manager loaded');
})();
