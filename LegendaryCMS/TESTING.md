# Testing Guide for LegendaryCMS

This guide provides step-by-step instructions for testing all features of the LegendaryCMS system.

## Prerequisites

1. Build the application:
   ```bash
   cd /home/runner/work/LegendaryCMS/LegendaryCMS
   dotnet build
   ```

2. Start the application:
   ```bash
   dotnet run
   ```

3. The application will be available at: http://localhost:5000

## Test Cases

### 1. Homepage Testing

**Test: Homepage Loads**
- Navigate to http://localhost:5000/
- ✅ Verify: Homepage displays with ASHAT OS CMS branding
- ✅ Verify: Feature cards are visible (Blogs, Forums, Profiles, Downloads, Admin Panel)
- ✅ Verify: Statistics show (Users, Posts, Blogs, Online Users)
- ✅ Verify: Login/Register buttons are visible (not logged in)

### 2. Authentication Testing

**Test: First Admin Registration**
- Click "Register" or navigate to http://localhost:5000/register
- ✅ Verify: Page shows "Admin Setup" badge
- ✅ Verify: Form has fields for username, email, password, confirm password
- Fill in the form:
  - Username: `admin`
  - Email: `admin@example.com`
  - Password: `admin123`
  - Confirm Password: `admin123`
- Click "Create Admin Account"
- ✅ Verify: Redirects to homepage
- ✅ Verify: Shows "Welcome, admin!" in header
- ✅ Verify: Admin Panel link is visible

**Test: User Registration**
- Logout (if logged in)
- Navigate to http://localhost:5000/register
- ✅ Verify: Page shows standard registration (not admin setup)
- Fill in the form:
  - Username: `testuser`
  - Email: `test@example.com`
  - Password: `test123`
  - Confirm Password: `test123`
- Click "Register"
- ✅ Verify: Redirects to login page

**Test: User Login**
- Navigate to http://localhost:5000/login
- Fill in the form:
  - Username: `testuser`
  - Password: `test123`
- Click "Login"
- ✅ Verify: Redirects to homepage
- ✅ Verify: Shows "Welcome, testuser!" in header

**Test: Logout**
- Click "Logout" button on homepage
- ✅ Verify: User is logged out
- ✅ Verify: Login/Register buttons appear again

### 3. Blog System Testing

**Test: View Blog List**
- Navigate to http://localhost:5000/cms/blogs
- ✅ Verify: Blog page loads with purple theme
- ✅ Verify: "Create New Post" button is visible (when logged in)
- ✅ Verify: Blog posts are listed (initially empty)
- ✅ Verify: Sidebar widgets are visible (Search, Categories, Recent Posts, Tags)

**Test: Create Blog Post (Requires Authentication)**
- Login as admin or testuser
- Navigate to http://localhost:5000/cms/blogs
- Click "✍️ Create New Post"
- ✅ Verify: Redirects to create page
- Fill in the form:
  - Title: "My First Blog Post"
  - Content: "This is the content of my first blog post. It has some <b>HTML</b> formatting."
  - Category: "General"
- Click "Publish"
- ✅ Verify: Post is created
- ✅ Verify: Redirects to the post page

**Test: View Blog Post**
- Navigate to http://localhost:5000/cms/blogs/post/1 (or the ID of created post)
- ✅ Verify: Post displays with title and content
- ✅ Verify: Author name is shown
- ✅ Verify: View count increases on page refresh
- ✅ Verify: Recent posts in sidebar

**Test: Edit Blog Post (Author/Admin Only)**
- Login as the post author
- Navigate to the blog post
- Click "Edit" (if button exists)
- Or navigate to http://localhost:5000/cms/blogs/create?id=1
- ✅ Verify: Form is pre-filled with post data
- Modify the content
- Click "Save Changes"
- ✅ Verify: Post is updated

### 4. User Control Panel Testing

**Test: Access Control Panel (Requires Authentication)**
- Login as any user
- Navigate to http://localhost:5000/control-panel
- ✅ Verify: Control panel page loads
- ✅ Verify: Current user data is displayed (username, email)
- ✅ Verify: Sidebar navigation is visible

**Test: Update Account Settings**
- In control panel, modify:
  - Email: `newemail@example.com`
  - Title: `Senior Developer`
  - Bio: `This is my bio text`
- Click "Save Changes"
- ✅ Verify: Success message appears
- ✅ Verify: Data is saved (refresh page to confirm)

**Test: Control Panel - Unauthenticated**
- Logout
- Navigate to http://localhost:5000/control-panel
- ✅ Verify: Redirects to login page

### 5. Forum System Testing

**Test: View Forums**
- Navigate to http://localhost:5000/cms/forums
- ✅ Verify: Forum page loads
- ✅ Verify: Forum categories are displayed (initially may be empty)
- ✅ Verify: Statistics are shown (threads, posts, members, online users)

### 6. Admin Panel Testing

**Test: Access Admin Panel (Admin Only)**
- Login as admin
- Navigate to http://localhost:5000/cms/admin
- ✅ Verify: Admin dashboard loads
- ✅ Verify: Statistics are displayed
- ✅ Verify: Navigation links work

**Test: Admin Panel - Non-Admin User**
- Login as a regular user (not admin)
- Navigate to http://localhost:5000/cms/admin
- ✅ Verify: Access is restricted or shows appropriate message

### 7. Profile System Testing

**Test: View User Profile**
- Navigate to http://localhost:5000/cms/profiles/admin
- ✅ Verify: Profile page loads
- ✅ Verify: User information is displayed
- ✅ Verify: Posts/activity (if any) are shown

### 8. API Endpoints Testing

**Test: Health Check**
- Navigate to http://localhost:5000/api/health
- ✅ Verify: JSON response with status, timestamp, system, and version

**Test: Auth Status**
- Login as any user
- Navigate to http://localhost:5000/api/auth/status
- ✅ Verify: JSON shows authenticated: true, username, role

**Test: Logout API**
- Use a tool like curl or Postman:
  ```bash
  curl -X POST http://localhost:5000/api/auth/logout -c cookies.txt -b cookies.txt
  ```
- ✅ Verify: Returns success message
- ✅ Verify: Session is cleared

### 9. Database Testing

**Test: Database Creation**
- Stop the application
- Delete `agp_cms.db` file
- Start the application
- ✅ Verify: Database file is created
- ✅ Verify: Tables are initialized
- Check tables:
  ```bash
  sqlite3 agp_cms.db ".tables"
  ```
- ✅ Verify: All tables exist (Users, BlogPosts, ForumCategories, etc.)

**Test: Database Persistence**
- Create a blog post
- Stop the application
- Start the application again
- Navigate to blogs page
- ✅ Verify: Blog post is still there

### 10. Security Testing

**Test: XSS Protection**
- Create a blog post with JavaScript:
  ```html
  <script>alert('XSS')</script>
  ```
- View the post
- ✅ Verify: Script is sanitized and doesn't execute

**Test: Session Expiry**
- Login
- Wait 24 hours (or manually delete session from database)
- Refresh page
- ✅ Verify: User is logged out

**Test: Unauthorized Access**
- Logout
- Navigate to http://localhost:5000/cms/blogs/create
- ✅ Verify: Redirects to login page

## Expected Results Summary

### Working Features ✅
1. ✅ Homepage with feature showcase
2. ✅ User registration and login
3. ✅ First-run admin account setup
4. ✅ Blog creation, viewing, and editing
5. ✅ User control panel
6. ✅ Account settings management
7. ✅ Session-based authentication
8. ✅ Database initialization and persistence
9. ✅ API endpoints (health, logout, auth status)
10. ✅ XSS protection
11. ✅ Role-based access control

### Partially Implemented Features ⚠️
1. ⚠️ Forum functionality (UI exists, needs backend implementation)
2. ⚠️ User profiles (basic implementation, needs social features)
3. ⚠️ Admin panel (UI exists, needs full management features)
4. ⚠️ Downloads (placeholder)

## Known Limitations

1. Password hashing uses basic SHA256 (should use BCrypt or Argon2 in production)
2. No email verification
3. No password reset functionality
4. No CSRF tokens (should add in production)
5. Forum posting not yet implemented in backend
6. Profile timeline/friends features not yet implemented
7. File upload functionality not implemented

## Troubleshooting

### Issue: Port Already in Use
**Solution:** Change port in `appsettings.json` or use:
```bash
dotnet run --urls "http://localhost:5555"
```

### Issue: Database Locked
**Solution:** Ensure no other process is accessing the database file. Stop all instances of the application.

### Issue: Session Not Persisting
**Solution:** Check that cookies are enabled in your browser.

### Issue: Can't Create Admin Account
**Solution:** Delete the database file and restart the application to trigger first-run setup.

## Success Criteria

The implementation is successful if:
- ✅ Application starts without errors
- ✅ Homepage loads and displays correctly
- ✅ Users can register and login
- ✅ First user gets admin role automatically
- ✅ Users can create and view blog posts
- ✅ Users can edit their account settings
- ✅ Sessions persist and expire correctly
- ✅ Database tables are created automatically
- ✅ API endpoints respond correctly
- ✅ Routes are properly mapped to /cms/* pattern
- ✅ Authentication is enforced for protected pages
