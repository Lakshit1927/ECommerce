# 🛒 E-Commerce Microservices Backend (.NET 9 + Minimal APIs)

A microservices-based backend system for an E-Commerce platform, built using ASP.NET Core Minimal APIs and Entity Framework Core.

---

## 📦 Services Included

### 🛍️ ProductService
- Manages product catalog
- CRUD operations (Create, Read, Update, Delete)
- Uses EF Core with InMemory or SQLite
- Swagger enabled

### 📦 OrderService
- Manages customer orders
- Stores order details with ProductId references
- Uses EF Core with SQLite database (`orders.db`)
- Swagger enabled

### 🎯 ApiGateway
- Built using YARP Reverse Proxy
- Routes:
  - `/products/*` → ProductService
  - `/orders/*` → OrderService
- Swagger enabled

---

## ⚙️ Tech Stack

- ASP.NET Core 9 Minimal APIs
- Entity Framework Core
- YARP Reverse Proxy
- Swagger / OpenAPI
- DTO-based API contracts
- Microservices Architecture

---

## 🚀 How to Run the Project

### 🔧 Prerequisites
- .NET 9 SDK
- Visual Studio or VS Code
- Postman / Swagger

### ▶️ Run Each Service Individually

#### 1. ProductService
```bash
cd ProductService
dotnet run --urls=http://localhost:5001
