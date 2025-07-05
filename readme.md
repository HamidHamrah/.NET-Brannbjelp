# 🔥 Net-Brannhjelp Backend (.NET 8 API)

This is the backend API for the Brannhjelp application, built with **.NET 8**. It provides REST endpoints for authentication, publication management, and user data.

## 🚀 Getting Started

### 📦 Prerequisites

- .NET 8 SDK
- Docker (optional)
- VSCode (recommended)

### 🔧 Setup Instructions

1. Clone the repository:
   ```bash
   git clone git@github.com:HamidHamrah/Net-Brannhjelp.git
   cd Net-Brannhjelp/Ignist
   ```

2. Restore and run:
   ```bash
   dotnet restore
   dotnet run --urls "http://0.0.0.0:8081"
   ```

3. Access Swagger UI:
   ```
   http://localhost:8081/swagger
   ```

## 🐳 Docker Instructions

1. Build the image:
   ```bash
   docker build -t net-brannhjelp-api .
   ```

2. Run the container:
   ```bash
   docker run -d -p 8081:8081 net-brannhjelp-api
   ```

## 🌐 Used By

- React-Brannhjelp frontend
- Any client that needs article/user management via API

## 🔒 Authentication

Uses JWT Bearer tokens for secured endpoints.
