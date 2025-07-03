# Backend API Documentation for Frontend Developers

## Authentication API

### 1. Login
- **Endpoint:** `/api/Auth/login`
- **Method:** POST
- **Authentication:** Not required
- **Description:** Authenticates a user and returns access and refresh tokens

**Request Body:**
```json
{
  "matricule": "910689",
  "password": "QrlaoazdEu"
}
```

**Response:**
```json
{
  "id": 1,
  "firstName": "Michael",
  "lastName": "Anderson",
  "matricule": "910689",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "EO62Xm9k+B5YEwqvS0K3u1mD7..."
}
```

**Status Codes:**
- 200 OK: Login successful
- 401 Unauthorized: Invalid credentials
- 500 Internal Server Error: Server error

**Notes:**
- The access token expires in 1 hour
- A secure HttpOnly cookie with the refresh token is also set
- Login attempts are recorded in the login history

### 2. Refresh Token
- **Endpoint:** `/api/Auth/refresh`
- **Method:** POST
- **Authentication:** Not required
- **Description:** Gets a new access token using a refresh token

**Request Body:**
```json
{
  "refreshToken": "EO62Xm9k+B5YEwqvS0K3u1mD7..."
}
```

**Response:**
```json
{
  "id": 1,
  "firstName": "Michael",
  "lastName": "Anderson",
  "matricule": "910689",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "sKY5tgY2FgGqfS8Bm3C7dHa9..."
}
```

**Status Codes:**
- 200 OK: Token refreshed successfully
- 400 Bad Request: Missing refresh token
- 401 Unauthorized: Invalid or expired refresh token
- 500 Internal Server Error: Server error

**Notes:**
- The refresh token cookie is automatically updated if present
- The endpoint accepts refresh token from either request body or cookie

### 3. Logout
- **Endpoint:** `/api/Auth/logout`
- **Method:** POST
- **Authentication:** Required (Bearer token)
- **Description:** Logs out a user by invalidating their refresh token

**Request Body:** None required

**Response:**
```json
{
  "message": "Logged out successfully"
}
```

**Status Codes:**
- 200 OK: Logout successful
- 400 Bad Request: Invalid user ID
- 401 Unauthorized: Missing/invalid token
- 500 Internal Server Error: Server error

**Notes:**
- Clears the refresh token cookie
- Invalidates the refresh token in the database

### 4. Get Current User
- **Endpoint:** `/api/Auth/me`
- **Method:** GET
- **Authentication:** Required (Bearer token)
- **Description:** Returns information about the currently authenticated user

**Request Body:** None required

**Response:**
```json
{
  "id": 1,
  "firstName": "Michael",
  "lastName": "Anderson",
  "matricule": "910689"
}
```

**Status Codes:**
- 200 OK: User information retrieved successfully
- 401 Unauthorized: Missing/invalid token
- 404 Not Found: User not found
- 500 Internal Server Error: Server error

## Login History API

### 1. Get All Login History
- **Endpoint:** `/api/LoginHistory`
- **Method:** GET
- **Authentication:** Required (Bearer token)
- **Description:** Returns the most recent login attempts across all users (limited to 100)

**Request Body:** None required

**Response:**
```json
[
  {
    "id": 123,
    "loginTime": "2025-07-03T18:30:45.123Z",
    "ipAddress": "192.168.1.1",
    "userAgent": "Mozilla/5.0...",
    "isSuccessful": true,
    "user": {
      "id": 1,
      "matricule": "910689",
      "firstName": "Michael",
      "lastName": "Anderson"
    }
  },
  // More login history records...
]
```

**Status Codes:**
- 200 OK: Login history retrieved successfully
- 401 Unauthorized: Missing/invalid token
- 500 Internal Server Error: Server error

### 2. Get User Login History
- **Endpoint:** `/api/LoginHistory/user/{userId}`
- **Method:** GET
- **Authentication:** Required (Bearer token)
- **Description:** Returns login history for a specific user (limited to 50)

**Parameters:**
- userId (path parameter): The ID of the user to get login history for

**Response:**
```json
[
  {
    "id": 123,
    "loginTime": "2025-07-03T18:30:45.123Z",
    "ipAddress": "192.168.1.1",
    "userAgent": "Mozilla/5.0...",
    "isSuccessful": true
  },
  // More login history records...
]
```

**Status Codes:**
- 200 OK: Login history retrieved successfully
- 401 Unauthorized: Missing/invalid token
- 500 Internal Server Error: Server error

### 3. Get Current User's Login History
- **Endpoint:** `/api/LoginHistory/my-history`
- **Method:** GET
- **Authentication:** Required (Bearer token)
- **Description:** Returns login history for the currently authenticated user (limited to 20)

**Request Body:** None required

**Response:**
```json
[
  {
    "id": 123,
    "loginTime": "2025-07-03T18:30:45.123Z",
    "ipAddress": "192.168.1.1",
    "userAgent": "Mozilla/5.0...",
    "isSuccessful": true
  },
  // More login history records...
]
```

**Status Codes:**
- 200 OK: Login history retrieved successfully
- 400 Bad Request: Invalid user ID
- 401 Unauthorized: Missing/invalid token
- 500 Internal Server Error: Server error

## Authentication Guide

### Setting Up Authentication

To make authenticated API requests, follow these steps:

1. **Obtain an access token** by calling the login endpoint
2. **Include the token in requests** using the Authorization header:
   ```
   Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```
3. **Handle token expiration** by using the refresh token endpoint when you receive a 401 response

### Example Authentication Flow

1. **Login and store tokens:**
   ```javascript
   async function login(matricule, password) {
     const response = await fetch('http://localhost:5000/api/Auth/login', {
       method: 'POST',
       headers: { 'Content-Type': 'application/json' },
       body: JSON.stringify({ matricule, password }),
       credentials: 'include'  // Important for cookies
     });
     
     if (response.ok) {
       const data = await response.json();
       localStorage.setItem('accessToken', data.accessToken);
       return data;
     }
     
     throw new Error('Login failed');
   }
   ```

2. **Make authenticated requests:**
   ```javascript
   async function getUserProfile() {
     const token = localStorage.getItem('accessToken');
     const response = await fetch('http://localhost:5000/api/Auth/me', {
       headers: {
         'Authorization': `Bearer ${token}`
       },
       credentials: 'include'
     });
     
     if (response.ok) {
       return await response.json();
     }
     
     if (response.status === 401) {
       // Token expired, try refreshing
       await refreshToken();
       return getUserProfile();
     }
     
     throw new Error('Failed to get profile');
   }
   ```

3. **Refresh token when needed:**
   ```javascript
   async function refreshToken() {
     const response = await fetch('http://localhost:5000/api/Auth/refresh', {
       method: 'POST',
       headers: { 'Content-Type': 'application/json' },
       credentials: 'include'  // The cookie will be sent automatically
     });
     
     if (response.ok) {
       const data = await response.json();
       localStorage.setItem('accessToken', data.accessToken);
       return data;
     }
     
     // If refresh fails, redirect to login
     window.location.href = '/login';
   }
   ```