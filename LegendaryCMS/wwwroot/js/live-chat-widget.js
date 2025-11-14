/**
 * Live Chat Widget - Customer Support
 * A simple, elegant live chat support widget for the homepage
 */
class LiveChatWidget {
    constructor() {
        this.isOpen = false;
        this.messages = [];
        this.init();
    }

    init() {
        this.createWidget();
        this.attachEventListeners();
        this.showWelcomeMessage();
    }

    createWidget() {
        // Create widget container
        const widgetHTML = `
            <div id="live-chat-widget" class="chat-widget">
                <!-- Chat Button -->
                <button id="chat-toggle-btn" class="chat-toggle-btn" aria-label="Toggle chat">
                    <svg class="chat-icon" viewBox="0 0 24 24" width="24" height="24">
                        <path fill="currentColor" d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H6l-2 2V4h16v12z"/>
                    </svg>
                    <svg class="close-icon" viewBox="0 0 24 24" width="24" height="24" style="display:none;">
                        <path fill="currentColor" d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
                    </svg>
                    <span class="chat-notification-badge" style="display:none;"></span>
                </button>

                <!-- Chat Window -->
                <div id="chat-window" class="chat-window" style="display:none;">
                    <div class="chat-header">
                        <div class="chat-header-content">
                            <div class="chat-avatar">💬</div>
                            <div class="chat-header-text">
                                <h3>Live Support</h3>
                                <span class="chat-status">
                                    <span class="status-dot"></span>
                                    Online
                                </span>
                            </div>
                        </div>
                        <button class="chat-minimize-btn" aria-label="Minimize chat">
                            <svg viewBox="0 0 24 24" width="20" height="20">
                                <path fill="currentColor" d="M19 13H5v-2h14v2z"/>
                            </svg>
                        </button>
                    </div>

                    <div class="chat-messages" id="chat-messages">
                        <!-- Messages will be inserted here -->
                    </div>

                    <div class="chat-input-container">
                        <input 
                            type="text" 
                            id="chat-input" 
                            class="chat-input" 
                            placeholder="Type your message..."
                            autocomplete="off"
                        />
                        <button id="chat-send-btn" class="chat-send-btn" aria-label="Send message">
                            <svg viewBox="0 0 24 24" width="20" height="20">
                                <path fill="currentColor" d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"/>
                            </svg>
                        </button>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', widgetHTML);
    }

    attachEventListeners() {
        const toggleBtn = document.getElementById('chat-toggle-btn');
        const minimizeBtn = document.querySelector('.chat-minimize-btn');
        const sendBtn = document.getElementById('chat-send-btn');
        const input = document.getElementById('chat-input');

        toggleBtn.addEventListener('click', () => this.toggleChat());
        minimizeBtn.addEventListener('click', () => this.toggleChat());
        sendBtn.addEventListener('click', () => this.sendMessage());
        input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.sendMessage();
            }
        });
    }

    toggleChat() {
        const chatWindow = document.getElementById('chat-window');
        const chatIcon = document.querySelector('.chat-icon');
        const closeIcon = document.querySelector('.close-icon');
        const badge = document.querySelector('.chat-notification-badge');
        
        this.isOpen = !this.isOpen;
        
        if (this.isOpen) {
            chatWindow.style.display = 'flex';
            chatIcon.style.display = 'none';
            closeIcon.style.display = 'block';
            badge.style.display = 'none';
            document.getElementById('chat-input').focus();
        } else {
            chatWindow.style.display = 'none';
            chatIcon.style.display = 'block';
            closeIcon.style.display = 'none';
        }
    }

    showWelcomeMessage() {
        setTimeout(() => {
            this.addMessage(
                'Hello! 👋 Welcome to AGP Studios CMS. How can we help you today?',
                'bot'
            );
        }, 1000);
    }

    sendMessage() {
        const input = document.getElementById('chat-input');
        const message = input.value.trim();
        
        if (!message) return;
        
        this.addMessage(message, 'user');
        input.value = '';
        
        // Simulate bot response
        setTimeout(() => {
            this.generateBotResponse(message);
        }, 1000);
    }

    addMessage(text, sender) {
        const messagesContainer = document.getElementById('chat-messages');
        const messageEl = document.createElement('div');
        messageEl.className = `chat-message ${sender}-message`;
        
        const timestamp = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        
        messageEl.innerHTML = `
            <div class="message-content">${this.escapeHtml(text)}</div>
            <div class="message-time">${timestamp}</div>
        `;
        
        messagesContainer.appendChild(messageEl);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
        
        this.messages.push({ text, sender, timestamp });
        
        // Show notification if chat is closed
        if (!this.isOpen && sender === 'bot') {
            this.showNotificationBadge();
        }
    }

    generateBotResponse(userMessage) {
        const lowerMessage = userMessage.toLowerCase();
        let response = '';
        
        if (lowerMessage.includes('hello') || lowerMessage.includes('hi') || lowerMessage.includes('hey')) {
            response = 'Hello! How can I assist you today? 😊';
        } else if (lowerMessage.includes('help')) {
            response = 'I\'m here to help! You can ask me about:\n• Blog features\n• Forum discussions\n• User profiles\n• Account management\n\nWhat would you like to know?';
        } else if (lowerMessage.includes('blog')) {
            response = 'Our blog system allows you to create and share articles with the community. Visit the Blogs section to get started!';
        } else if (lowerMessage.includes('forum')) {
            response = 'Join our forums to engage in discussions and connect with the community. Check out the Forums section!';
        } else if (lowerMessage.includes('account') || lowerMessage.includes('register') || lowerMessage.includes('login')) {
            response = 'You can register for a new account or login to access all features. Click the Register or Login buttons at the top of the page.';
        } else if (lowerMessage.includes('price') || lowerMessage.includes('cost') || lowerMessage.includes('pricing')) {
            response = 'We offer different packages:\n• Forums Hosting: $5\n• CMS with Blogs and Forums: $20\n\nFor more details, please contact our sales team.';
        } else if (lowerMessage.includes('thank')) {
            response = 'You\'re welcome! Feel free to ask if you have any other questions. 😊';
        } else if (lowerMessage.includes('bye') || lowerMessage.includes('goodbye')) {
            response = 'Goodbye! Have a great day! Feel free to return if you have more questions. 👋';
        } else {
            response = 'Thanks for your message! For more specific assistance, please:\n• Check our documentation\n• Contact support at support@agpstudios.com\n• Or ask me another question!';
        }
        
        this.addMessage(response, 'bot');
    }

    showNotificationBadge() {
        const badge = document.querySelector('.chat-notification-badge');
        badge.style.display = 'block';
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML.replace(/\n/g, '<br>');
    }
}

// Initialize the chat widget when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        new LiveChatWidget();
    });
} else {
    new LiveChatWidget();
}
