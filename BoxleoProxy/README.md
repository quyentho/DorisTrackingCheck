# BoxleoProxy - Backend for Frontend

This .NET 8 Web API acts as a secure proxy between your frontend and the Boxleo API, handling authentication and token management transparently.

## Features

- **Automatic Token Management**: Authenticates with Boxleo and caches the bearer token
- **Auto-Refresh**: Automatically refreshes expired tokens when detecting 401 responses
- **Thread-Safe**: Uses locking to prevent concurrent refresh attempts
- **CORS Enabled**: Allows requests from your frontend

## Setup

### 1. Configure Credentials

Edit `appsettings.json` and replace the placeholders with your actual Boxleo credentials:

```json
{
  "Boxleo": {
    "BaseUrl": "https://boxleo-backend-nml82.ondigitalocean.app",
    "LoginUrl": "https://boxleo-backend-nml82.ondigitalocean.app/api/auth/login",
    "Email": "your-actual-email@example.com",
    "Password": "your-actual-password",
    "TokenExpiryHours": 24
  }
}
```

**IMPORTANT**: For production, use environment variables or Azure Key Vault instead of storing credentials in `appsettings.json`.

### 2. Run the Server

```bash
cd BoxleoProxy
dotnet run
```

The API will start at:

- HTTP: `http://localhost:5028`
- HTTPS: `https://localhost:7251`

## API Endpoints

### GET /api/boxleo/orders

Fetches orders from Boxleo as JSON with automatic authentication.

**Query Parameters:**

- `page` (default: 1)
- `per_page` (default: 15)
- `orders_type` (default: "leads")
- `is_marketplace` (default: "all")

**Example:**

```
GET http://localhost:5028/api/boxleo/orders?page=1&per_page=15&orders_type=leads&is_marketplace=all
```

### GET /api/boxleo/orders/csv

Fetches orders from Boxleo and returns them as a CSV file with automatic authentication.

**Query Parameters:**

- `page` (default: 1)
- `per_page` (default: 15)
- `orders_type` (default: "leads")
- `is_marketplace` (default: "all")

**CSV Format:**

- Order ID, Customer name, Customer Number, Address, Products, Price, Status, Delivery date, Comments
- Each product in an order generates a separate row
- Delivery date is determined based on the shipping status

**Example:**

```
GET http://localhost:5028/api/boxleo/orders/csv?page=1&per_page=15&orders_type=leads&is_marketplace=all
```

## How It Works

1. **First Request**: Frontend calls `/api/boxleo/orders` or `/api/boxleo/orders/csv`
2. **Authentication**: Backend authenticates with Boxleo using configured credentials
3. **Token Caching**: Backend stores the token in memory with expiration time
4. **Proxying**: Backend forwards the request to Boxleo with the bearer token
5. **Auto-Refresh**: If Boxleo returns 401, backend automatically refreshes the token and retries
6. **CSV Transformation** (for `/csv` endpoint): Transforms JSON response to CSV format with field mappings

## Docker Deployment

### Using Docker Compose

The project includes Docker support and integrates with the main application's nginx proxy.

1. **Update docker-compose.yml** (already configured in the root folder):

```yaml
services:
  boxleo_proxy:
    build:
      context: ./BoxleoProxy
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - Boxleo__Email=your-email@example.com
      - Boxleo__Password=your-password
    expose:
      - "80"
```

2. **Build and run**:

```bash
docker-compose up -d
```

The frontend will access the API through nginx at `/api/boxleo/` which proxies to the `boxleo_proxy` service.

### Nginx Configuration

The nginx.conf includes a location block for the Boxleo proxy:

```nginx
location ^~ /api/boxleo/ {
    proxy_pass         http://boxleo_proxy/api/boxleo/;
    proxy_http_version 1.1;
    proxy_set_header   Upgrade $http_upgrade;
    proxy_set_header   Connection keep-alive;
    proxy_set_header   Host $host;
    proxy_cache_bypass $http_upgrade;
    proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header   X-Forwarded-Proto $scheme;
}
```

## Security Notes

- The frontend never sees or handles Boxleo credentials
- Tokens are cached in-memory on the backend
- Concurrent refresh requests are handled with thread-safe locking
- CORS is currently set to `AllowAll` - restrict this in production

## Production Deployment

For production:

1. Use environment variables for credentials (in docker-compose.yml or your orchestration platform)

2. Update CORS policy in `Program.cs` to allow only your frontend domain

3. Enable HTTPS and configure proper certificates

4. Consider using a distributed cache (Redis) for token storage in multi-instance deployments

5. Set appropriate `per_page` limits to prevent excessive data loads
