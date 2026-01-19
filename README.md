# Communication.Tech

**MSc Thesis Project**: Performance Comparison Platform for Multiple Communication Protocols

> A comprehensive analysis of HTTP REST, gRPC, Kafka, RabbitMQ, and Redis as communication and queuing mechanisms under various load conditions.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Installation & Setup](#installation--setup)
- [Project Structure](#project-structure)
- [API Documentation](#api-documentation)
- [Performance Metrics](#performance-metrics)
- [Performance Testing](#-performance-testing)
- [License](#license)

---

## 🎯 Overview

Communication.Tech is a microservices-based platform designed to evaluate and compare the performance characteristics of different communication protocols and message queue systems:

- **HTTP/1.1** - Traditional synchronous communication
- **HTTP/2** - Improved HTTP protocol with multiplexing and header compression
- **gRPC** - High-performance RPC framework
- **WebSocket** - Real-time bidirectional communication (basic implementation)
- **GraphQL** - Query language for APIs (basic implementation)
- **Kafka** - Distributed event streaming platform
- **RabbitMQ** - Message broker for asynchronous communication
- **Redis** - In-memory data structure store (used as queue)

This project provides real-world benchmarking under controlled load conditions, helping developers make informed decisions about communication protocol selection.

---

## ✨ Features

- ✅ **Multi-Protocol Support** - HTTP/1.1, HTTP/2, gRPC, WebSocket, GraphQL, Kafka, RabbitMQ, Redis
- ✅ **Load Testing** - Configurable load scenarios
- ✅ **Performance Metrics** - Latency, throughput, resource utilization tracking
- ✅ **Microservices Architecture** - Gateway, Server, and Consumer services
- ✅ **Containerized Deployment** - Docker and Docker Compose
- ✅ **Real-time Monitoring** - Prometheus integration (docker-stack.yaml)
- ✅ **Async Messaging** - Event-driven architecture support

---

## 🛠 Technology Stack

### Core Framework
- **.NET 8+** - C# runtime
- **ASP.NET Core** - Web API framework

### Communication Protocols
- **gRPC** - Protocol Buffers for RPC
- **HTTP/1.1** - Traditional synchronous REST communication
- **HTTP/2** - Improved HTTP protocol with multiplexing and header compression (basic implementation)
- **WebSocket** - Real-time bidirectional communication (basic implementation)
- **GraphQL** - Query language for APIs (basic implementation)

### Message Brokers
- **Apache Kafka** - Distributed event streaming
- **RabbitMQ** - AMQP message broker
- **Redis** - In-memory queue system

### Infrastructure
- **Docker** - Containerization
- **Docker Compose** - Multi-container orchestration
- **Monitoring:** Prometheus integrated

### Docker Stack Deployment
- **Orchestration:** Docker Swarm
- **Configuration:** docker-stack.yaml
- **Services:** Gateway, Server, Consumer, Kafka, RabbitMQ, Redis


### Docker Swarm Requirements

- Docker Engine 20.10+
- Single host or multi-node Swarm cluster
- Swarm mode enabled (docker swarm init)
- **Prometheus** - Metrics collection and monitoring

---

## 🏗 Architecture

### Base Architecture

![communication-workflow](https://github.com/user-attachments/assets/1b2b34d1-30a8-4d8b-b392-d56b53c01390)

**Components:**
- **Gateway** - Entry point for requests, routes to appropriate service
- **Server** - Main API service handling HTTP REST and gRPC endpoints
- **Consumer** - Consumes messages from Kafka, RabbitMQ, and Redis queues

### AWS Sample Architecture

![aws-architecture](https://github.com/user-attachments/assets/a2fab25a-317e-471c-afc7-a8b279e7ca3f)

This diagram shows how the microservices can be deployed on AWS infrastructure with load balancers, auto-scaling groups, and managed services.

---

## 📦 Prerequisites

Before you begin, ensure you have the following installed:

- **Docker Desktop** (includes Docker and Docker Compose)
- **.NET 6 SDK or later** (if running without containers)
- **Git** (for cloning the repository)
- **4GB RAM minimum** (recommended 8GB+ for full testing)
- **2GB free disk space** (for containers and dependencies)

### Verify Installation

```bash
docker --version          # Should be 20.10+
docker-compose --version  # Should be 1.29+
dotnet --version          # Should be 6.0+
```

---

## 🚀 Installation & Setup

### Option 1: Docker Compose (Recommended)

**1. Clone the Repository**

```bash
git clone https://github.com/berkcanc/Communication.Tech.git
cd Communication.Tech
```

**2. Start Services**

```bash
docker-compose up -d
```

This command starts:
- API Gateway (port 5000)
- Server (port 5001)
- Consumer (port 5002)
- Kafka
- RabbitMQ
- Redis
- Prometheus (optional, via docker-stack.yaml)

**3. Verify Services**

```bash
docker-compose ps
```

All services should show `Up` status.

### Option 2: Local Development

**1. Clone the Repository**

```bash
git clone https://github.com/berkcanc/Communication.Tech.git
cd Communication.Tech
```

**2. Build the Solution**

```bash
dotnet build
```

**3. Run Each Service**

Terminal 1 - Gateway:
```bash
cd Communication.Tech.Gateway
dotnet run
```

Terminal 2 - Server:
```bash
cd Communication.Tech.Server
dotnet run
```

Terminal 3 - Consumer:
```bash
cd Communication.Tech.Consumer
dotnet run
```

**4. Start Message Brokers**

```bash
# Make sure Docker is running, then start only the brokers
docker-compose up kafka rabbitmq redis -d
```

---


## 📁 Project Structure

```
Communication.Tech/
├── Communication.Tech.Consumer/
│   ├── bin/                      # Compiled binaries
│   ├── Consumers/                # Message consumer implementations
│   ├── Interfaces/               # Consumer interfaces
│   ├── Models/                   # Data models
│   ├── obj/                      # Object files
│   ├── Properties/               # Project properties
│   ├── Services/                 # Consumer services
│   ├── appsettings.Development.json
│   ├── appsettings.json          # Configuration
│   ├── Communication.Tech.Consumer.csproj
│   ├── Dockerfile                # Container image definition
│   ├── NoDelayTcpClient.cs       # TCP client with no-delay option
│   └── Program.cs                # Consumer entry point
│
├── Communication.Tech.Gateway/
│   ├── bin/                      # Compiled binaries
│   ├── Controllers/              # API endpoints
│   ├── Enums/                    # Enumeration types
│   ├── Helper/                   # Helper utilities
│   ├── Interfaces/               # Gateway interfaces
│   ├── Middlewares/              # Custom middlewares
│   ├── Models/                   # Data models
│   ├── obj/                      # Object files
│   ├── Properties/               # Project properties
│   ├── Protos/                   # gRPC proto files
│   ├── Services/                 # Gateway services
│   ├── appsettings.Development.json
│   ├── appsettings.json          # Configuration
│   ├── communication-tech.http   # HTTP request testing file
│   ├── Communication.Tech.Gateway.csproj
│   ├── Constants.cs              # Application constants
│   ├── Dockerfile                # Container image definition
│   ├── NoDelayTcpClient.cs       # TCP client with no-delay option
│   └── Program.cs                # Gateway entry point
│
├── Communication.Tech.Server/
│   ├── bin/                      # Compiled binaries
│   ├── Controllers/              # REST API endpoints
│   ├── Interfaces/               # Server interfaces
│   ├── Models/                   # Data models
│   ├── obj/                      # Object files
│   ├── Properties/               # Project properties
│   ├── Protos/                   # gRPC proto files
│   ├── Services/                 # Business logic services
│   ├── appsettings.Development.json
│   ├── appsettings.json          # Configuration
│   ├── BookRepository.cs         # Data repository
│   ├── Communication.Tech.Server.csproj
│   ├── Communication.Tech.Server.http  # HTTP request testing file
│   ├── Constants.cs              # Application constants
│   ├── Dockerfile                # Container image definition
│   ├── Mutation.cs               # GraphQL mutations (basic)
│   ├── Program.cs                # Server entry point
│   └── Query.cs                  # GraphQL queries (basic)
│
├── .dockerignore                 # Docker build ignore patterns
├── .gitignore                    # Git ignore patterns
├── communication-tech.sln        # Visual Studio solution file
├── docker-compose.yaml           # Docker Compose orchestration
├── docker-stack.yaml             # Docker Stack with monitoring & Prometheus
├── prometheus.yml                # Prometheus scrape configuration
└── README.md                      # Project documentation
```

### Key Files Explanation

• **Controllers/** - REST API endpoint handlers (HTTP, WebSocket, GraphQL)

• **Services/** - Business logic and protocol implementations

• **Models/** - Data transfer objects and entity models

• **Protos/** - Protocol Buffer definitions for gRPC

• **Interfaces/** - Service contracts and abstractions

• **.http files** - REST Client request collections (VS Code REST Client)

• **Dockerfile** - Container images for each microservice

• **Constants.cs** - Application-wide constants and configurations

• **NoDelayTcpClient.cs** - TCP optimization for low-latency communication

• **Query.cs & Mutation.cs** - GraphQL schema definitions (basic implementation)

• **appsettings.json** - Service configuration (database, ports, logging)

• **Program.cs** - Service initialization and dependency injection

---

## 📚 API Documentation

### Gateway Base URL
```
http://localhost:5000
```

### Server Base URL
```
http://localhost:5001
```

---

## Gateway API Endpoints

### HTTP Protocol Testing

#### GET `/HTTP`

Send a message via HTTP/1.1 protocol to the server and measure performance metrics.

**Query Parameters:**

• `message` (string, required) - Message content to send  
• `sizeInKB` (integer, required) - Payload size in kilobytes

**Response:**

Returns an `ApiResponse` object containing the message and server response timestamp.

**Example Request:**

```bash
curl "http://localhost:5000/HTTP?message=hello&sizeInKB=1"
```

**Example Response:**

```json
{
  "message": "hello",
  "receivedAt": "2024-01-15T10:30:45Z"
}
```

**Status Codes:**

• `200 OK` - Message sent successfully  
• `400 Bad Request` - Invalid parameters  
• `500 Internal Server Error` - Server error

---

### HTTP/2 Protocol Testing

#### GET `/HTTP2`

Send a message via HTTP/2 protocol to the server and measure performance metrics.

**Query Parameters:**

• `message` (string, required) - Message content to send  
• `sizeInKB` (integer, required) - Payload size in kilobytes

**Response:**

Returns an `ApiResponse` object containing the message and server response timestamp.

**Example Request:**

```bash
curl "http://localhost:5000/HTTP2?message=hello&sizeInKB=1"
```

**Example Response:**

```json
{
  "message": "hello",
  "receivedAt": "2024-01-15T10:30:45Z"
}
```

**Status Codes:**

• `200 OK` - Message sent successfully  
• `400 Bad Request` - Invalid parameters  
• `500 Internal Server Error` - Server error

---

### gRPC Protocol Testing

#### GET `/Grpc/sayhello`

Send a message via gRPC protocol to the server for high-performance RPC communication.

**Query Parameters:**

• `message` (string, required) - Message content to send  
• `sizeInKB` (integer, required) - Payload size in kilobytes

**Response:**

Message response from gRPC server containing the echoed message.

**Example Request:**

```bash
curl "http://localhost:5000/Grpc/sayhello?message=hello&sizeInKB=1"
```

**Example Response:**

```json
{
  "message": "hello"
}
```

**Status Codes:**

• `200 OK` - Message sent successfully via gRPC  
• `400 Bad Request` - Invalid parameters  
• `500 Internal Server Error` - gRPC connection error

---

### Kafka Message Producer

#### POST `/Kafka/produce`
**Description:** Send a message to Kafka topic for asynchronous processing

**Request Body:**
```json
{
  "message": "your message",
  "sizeInKB": 1
}
```

**Required Fields:**
- `message` (string) - Message content (cannot be empty)
- `sizeInKB` (integer) - Payload size in kilobytes

**Response:**
```json
"Message sent to Kafka."
```

**Example Request:**
```bash
curl -X POST http://localhost:5000/Kafka/produce \
  -H "Content-Type: application/json" \
  -d '{
    "message": "performance test message",
    "sizeInKB": 5
  }'
```

**Status Codes:**
- `200 OK` - Message sent to Kafka successfully
- `400 Bad Request` - Message is empty or invalid
- `500 Internal Server Error` - Kafka connection error

---

### RabbitMQ Message Producer

#### POST `/RabbitMQ/produce`
**Description:** Send a message to RabbitMQ queue for asynchronous processing

**Request Body:**
```json
{
  "message": "your message",
  "sizeInKB": 1
}
```

**Required Fields:**
- `message` (string) - Message content (cannot be empty)
- `sizeInKB` (integer) - Payload size in kilobytes

**Response:**
```json
"Message sent to RabbitMQConsumer."
```

**Example Request:**
```bash
curl -X POST http://localhost:5000/RabbitMQ/produce \
  -H "Content-Type: application/json" \
  -d '{
    "message": "performance test message",
    "sizeInKB": 3
  }'
```

**Status Codes:**
- `200 OK` - Message sent to RabbitMQ successfully
- `400 Bad Request` - Message is empty or invalid
- `500 Internal Server Error` - RabbitMQ connection error

---

### Redis Queue Operations

#### POST `/Redis/enqueue`
**Description:** Add a message to Redis queue and record latency metrics

**Request Body:**
```json
{
  "message": "your message",
  "sizeInKB": 1
}
```

**Required Fields:**
- `message` (string) - Message content
- `sizeInKB` (integer) - Payload size in kilobytes

**Response:**
```json
{
  "status": "queued",
  "messageId": "550e8400-e29b-41d4-a716-446655440000",
  "payload": "your message",
  "metrics": {
    "lpushLatencyMs": 0.123,
    "setLatencyMs": 0.045,
    "totalResponseTimeMs": 0.168
  }
}
```

**Example Request:**
```bash
curl -X POST http://localhost:5000/Redis/enqueue \
  -H "Content-Type: application/json" \
  -d '{
    "message": "test message",
    "sizeInKB": 2
  }'
```

**Status Codes:**
- `200 OK` - Message queued successfully
- `400 Bad Request` - Invalid request
- `500 Internal Server Error` - Redis connection error

#### GET `/Redis/count`
**Description:** Get the current message count in Redis queue

**Response:**
```json
{
  "messageCount": 42
}
```

**Example Request:**
```bash
curl http://localhost:5000/Redis/count
```

**Status Codes:**
- `200 OK` - Queue count retrieved successfully
- `500 Internal Server Error` - Redis connection error

---

### GraphQL Queries

#### GET `/GraphQL` (Basic Implementation)
**Description:** Execute a GraphQL query to retrieve books

**Response:**
```json
{
  "data": {
    "books": [
      {
        "title": "Book Title",
        "author": "Author Name"
      }
    ]
  }
}
```

**Query Executed:**
```graphql
query {
  books {
    title
    author
  }
}
```

**Example Request:**
```bash
curl http://localhost:5000/GraphQL
```

**Status Codes:**
- `200 OK` - Query executed successfully
- `400 Bad Request` - Query error
- `500 Internal Server Error` - Server error

---

### Prometheus Metrics Export

#### GET `/Data/export/ExportCsv`
**Description:** Export Prometheus metrics as CSV file

**Query Parameters:**
- `query` (string, required) - Prometheus query (e.g., `request_duration_seconds`)
- `startTime` (DateTime, optional) - Start time in Turkey timezone
- `endTime` (DateTime, optional) - End time in Turkey timezone
- `step` (string, optional) - Data point interval (default: `5s`)

**Response:** CSV file download

**Example Request:**
```bash
curl "http://localhost:5000/Data/export/ExportCsv?query=request_duration_seconds&startTime=2024-01-15T10:00:00&endTime=2024-01-15T11:00:00&step=10s" \
  -o metrics_export.csv
```

**CSV Format:**
```
request_duration_seconds
Average Metric Value
5.42 ms
```

**Status Codes:**
- `200 OK` - CSV exported successfully
- `404 Not Found` - No data found for given range
- `500 Internal Server Error` - Export error

---

#### POST `/Data/export/collect`
**Description:** Collect and store metrics for a specific technology

**Request Body:**
```json
{
  "technologyId": 1,
  "tps": 60000, // 1000 TPS
  "payloadSize": 5
}
```

**Required Fields:**

• `technologyId` (integer) - Technology ID:

  • `1` = HTTP 

  • `2` = gRPC

  • `3` = Redis

  • `4` = RabbitMQ

  • `5` = Kafka

• `tps` (integer) - Transactions per second 

(value in milliseconds, e.g., 60000 ms = 1000 TPS)

• `payloadSize` (integer) - Payload size in KB

**Response:**
```json
{
  "message": "Metric collected and stored for Http",
  "technologyName": "Http",
  "status": "Success",
  "tps": 60000,
  "payloadSize": 5
}
```

**Example Request:**
```bash
curl -X POST http://localhost:5000/Data/export/collect \
  -H "Content-Type: application/json" \
  -d '{
    "technologyId": 1,
    "tps": 60000,
    "payloadSize": 10
  }'
```

**Status Codes:**
- `200 OK` - Metrics collected successfully
- `400 Bad Request` - Invalid technology ID or request format
- `500 Internal Server Error` - Metrics collection error

---

## Server API Endpoints

### HTTP Server Receiver

#### POST `/HTTPServer`

Receive and process messages sent via HTTP/1.1 protocol from the gateway.

**Request Body:**

```json
{
  "message": "your message content"
}
```

**Required Fields:**

• `message` (string, required) - The message content to process

**Response:**

Returns the received message in JSON format.

**Example Request:**

```bash
curl -X POST http://localhost:5001/HTTPServer \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Hello from HTTP"
  }'
```

**Example Response:**

```json
{
  "message": "Hello from HTTP"
}
```

**Status Codes:**

• `200 OK` - Message received and processed successfully  
• `400 Bad Request` - Invalid request format  
• `500 Internal Server Error` - Server processing error

---

### HTTP/2 Server Receiver

#### POST `/HTTP2Server`

Receive and process messages sent via HTTP/2 protocol from the gateway.

**Request Body:**

```json
{
  "message": "your message content"
}
```

**Required Fields:**

• `message` (string, required) - The message content to process

**Response:**

Returns the received message in JSON format.

**Example Request:**

```bash
curl -X POST http://localhost:5001/HTTP2Server \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Hello from HTTP/2"
  }'
```

**Example Response:**

```json
{
  "message": "Hello from HTTP/2"
}
```

**Status Codes:**

• `200 OK` - Message received and processed successfully  
• `400 Bad Request` - Invalid request format  
• `500 Internal Server Error` - Server processing error

### Kafka
- **Topics:** Messages produced to Kafka topic for consumer processing
- **Producer:** Via `/Kafka/produce` endpoint
- **Consumer:** Handled by Consumer service

### RabbitMQ
- **Queues:** Messages sent to RabbitMQ broker
- **Producer:** Via `/RabbitMQ/produce` endpoint
- **Consumer:** Handled by Consumer service

### Redis
- **Data Structure:** List-based queue with `message_queue` key
- **Operations:** LPUSH for enqueue, RPOP for dequeue (by Consumer)
- **Producer:** Via `/Redis/enqueue` endpoint
- **Consumer:** Handled by Consumer service


---

## 📊 Performance Metrics

The platform tracks the following metrics for each protocol:

• **Throughput** - Messages processed per second (TPS)

• **Latency** - Time from request initiation to response receipt (ms)

• **Response Time** - Total time to process a message including all operations (ms)

• **Turnaround Time** - End-to-end time from message enqueue to dequeue (ms)

• **CPU Usage (%)** - Percentage of CPU resources consumed

• **Memory Usage (%)** - Percentage of memory resources consumed

### Accessing Metrics

Via Prometheus (if using docker-stack.yaml):
```
http://localhost:9090/metrics
```

Via API:
```bash
curl http://localhost:5001/api/metrics
```

---

## 📈 Performance Testing
Execute on JMeter AWS Instance to Gateway AWS Instance.
### Load Test For Query Params Tech.

```bash
,#!/bin/bash

JMETER_BIN="/home/ubuntu/jmeter/bin/jmeter"
TEST_PLAN="/home/ubuntu/jmeter/Redis-Request-With-Parameter.jmx" # Kafka, RabbitMQ and Redis JMeter Test Plan Input
THREADS=1000
RAMP_TIME=1
DURATION=3600  # seconds
TARGET_URL="http://ec2-63-177-92-241.eu-central-1.compute.amazonaws.com:6060/Redis/enqueue"
EXPORT_URL="http://ec2-63-177-92-241.eu-central-1.compute.amazonaws.com:6060/Data/export/collect"

TECHNOLOGY_ID=3

# Parameter combinations
THROUGHPUTS=(30000)
MESSAGE_SIZES=(0 1 5)

for TH in "${THROUGHPUTS[@]}"; do
  for MS in "${MESSAGE_SIZES[@]}"; do

    echo "=================================================================="
    echo "=== Running test: THROUGHPUT=${TH}, MESSAGE_SIZE=${MS}, DURATION=${DURATION} ==="
    echo "=================================================================="

    $JMETER_BIN -n \
      -t "$TEST_PLAN" \
      -JTHREADS=$THREADS \
      -JRAMP_TIME=$RAMP_TIME \
      -JDURATION=$DURATION \
      -JTHROUGHPUT=$TH \
      -JMESSAGE_SIZE=$MS \
      -JTARGET_URL="$TARGET_URL"

    echo "=== Test completed ==="

    echo "Waiting 6 seconds for Prometheus scrape..."
    sleep 6

    # --- Export Loop For Scrape Interval (3 Times) ---
    for i in {1..3}
    do
      echo "----------------------------------------"
      echo "Triggering export to Gateway (Round $i/3)..."

      curl -X POST "$EXPORT_URL" \
        -H "accept: */*" \
        -H "Content-Type: application/json" \
        -d "{\"technologyId\":${TECHNOLOGY_ID},\"tps\":${TH},\"payloadSize\":${MS}}"

      echo ""
      echo "=== Export completed for Round $i ==="

      # Wait to prevent file writing conflicts
      echo "Sleeping 6 seconds before next request..."
      sleep 6
    done
    # ----------------------------------

  done
done

echo "All tests finished!"

```

### Load Test For Message Brokers

```bash
JMETER_BIN="/home/ubuntu/jmeter/bin/jmeter"
TEST_PLAN="/home/ubuntu/jmeter/Query-Params-Request.jmx" # For HTTP and gRPC
THREADS=1000
RAMP_TIME=1
DURATION=3600  # seconds
TARGET_URL="http://ec2-63-177-92-241.eu-central-1.compute.amazonaws.com:6060/HTTP"
EXPORT_URL="http://ec2-63-177-92-241.eu-central-1.compute.amazonaws.com:6060/Data/export/collect"
TECHNOLOGY_ID=1
MESSAGE="Hello HTTP"

# Parameter combinations
THROUGHPUTS=(30000)
MESSAGE_SIZES=(0 1 5)

for TH in "${THROUGHPUTS[@]}"; do
  for MS in "${MESSAGE_SIZES[@]}"; do
 
    echo "=== Running test: THROUGHPUT=${TH}, MESSAGE_SIZE=${MS}, DURATION=${DURATION} ==="

    $JMETER_BIN -n \
        -t "$TEST_PLAN" \
        -JTHREADS=$THREADS \
        -JRAMP_TIME=$RAMP_TIME \
        -JDURATION=$DURATION \
        -JTHROUGHPUT=$TH \
        -JMESSAGE="$MESSAGE" \
        -JMESSAGE_SIZE=$MS \
        -JTARGET_URL="$TARGET_URL"

    echo "=== Test completed ==="

    # --- Export Loop For Scrape Interval (3 Times) ---
    for i in {1..3}
    do

        echo "Triggering export to Gateway..."
        curl -X POST "$EXPORT_URL" \
           -H "accept: */*" \
           -H "Content-Type: application/json" \
           -d "{\"technologyId\":${TECHNOLOGY_ID},\"tps\":${TH},\"payloadSize\":${MS}}"
        echo
        echo "=== Export completed ==="

        # Wait to prevent file writing conflicts
        echo "Sleeping 6 seconds before next request..."
        sleep 6
    done
  done
done

```


## 🐳 Docker Stack Commands Reference

### Stack Management

```bash
# Deploy the stack
docker stack deploy -c docker-stack.yaml communication-tech

# List all stacks
docker stack ls

# View stack services
docker stack services communication-tech

# View stack tasks
docker stack ps communication-tech

# Remove the stack
docker stack rm communication-tech
```

### Service Management

```bash
# View service status
docker service ls

# View service details
docker service inspect communication-tech_server

# View service logs
docker service logs communication-tech_server

# Scale a service
docker service scale communication-tech_server=3

# Update service
docker service update --image newimage:tag communication-tech_server
```

### Node Management

```bash
# List nodes in swarm
docker node ls

# View node details
docker node inspect <node-id>
```

### Monitoring & Debugging

```bash
# View stack events
docker events --filter type=service

# Check service health
curl http://localhost:5000/api/health

# View Prometheus metrics
curl http://localhost:9090/metrics

# Access service container
docker exec -it <container-id> bash
```

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👤 Author

**Berkcan Çiftçi**

- GitHub: [@berkcanc](https://github.com/berkcanc)
- Project: [Communication.Tech](https://github.com/berkcanc/Communication.Tech)

---

**Last Updated:** January 2026  
**Version:** 1.0.0
