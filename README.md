# examScheduler

examScheduler is a .NET 9.0 exam scheduling and management system designed to simplify the process of creating and maintaining comprehensive exam timetables for schools, universities, or institutions. It combines automation, data integrity, and flexibility with a modular architecture built on modern .NET technologies.

## Features

- JWT-based authentication and authorization
- Entity Framework Core integration with PostgreSQL
- Multi-tenant data structure with entities for Students, Teachers, Classrooms, Schedules, Subjects, and Exam Slots
- Automatic conflict detection and scheduling logic (in progress)
- Docker & Aspire (.NET) for containerized orchestration
- Clear layering via multiple projects (API, Entities, Utilities, Client Registration)
- Ready-to-extend test project for CI/CD and regression testing

## Architecture Overview

The solution is divided into several key projects:

| Project | Description |
|----------|-------------|
| **examScheduler** | Main ASP.NET Core Web API project handling routing, authentication, and API endpoints. |
| **Entities** | Contains all EF Core data models and relationships, including Student, Teacher, Schedule, Subject, etc. |
| **Models** | Provides data transfer objects (DTOs) used for API communication. |
| **RegisterClient** | Handles external registration or user onboarding logic. |
| **Util** | Contains helper utilities and shared logic. |
| **examScheduler.AppHost** | .NET Aspire orchestration layer for distributed hosting and service management. |
| **examScheduler.ServiceDefaults** | Defines shared configuration defaults across services. |
| **test** | Test project for integration and unit testing of the scheduler API and logic. |

## Technologies Used

- **.NET 9.0** (ASP.NET Core Web API)
- **Entity Framework Core 9** (with PostgreSQL)
- **JWT Bearer Authentication**
- **Argon2 password hashing** via `Isopoh.Cryptography.Argon2`
- **Docker & docker-compose** for deployment
- **.NET Aspire** for orchestrated app hosting
- **OpenAPI (Swagger)** documentation

## Current Progress

As of October 2025:
- Teacher and Subject relationship mapping completed.
- Calendar parsing and database schema refined with unique indexes.
- UserProfile entity now unifies teachers and students.
- Authentication is functional via AuthController.
- Testing framework scaffolded but pending implementation.

## Project Roadmap

1. Build remaining API controllers: Schedule, ExamSlot, Classroom, Student, Teacher.
2. Implement scheduling algorithm for automatic timetable generation.
3. Add frontend (Blazor, React, or Angular).
4. Expand test coverage for business logic and integration flows.
5. Complete Aspire orchestration and add CI/CD via GitHub Actions.

## Setup Instructions

1. **Clone the repository**
   ```bash
   git clone https://github.com/rollingCheeseWheel/examScheduler.git
   cd examScheduler
   ```

2. **Configure database connection**
   Add your PostgreSQL connection string to `appsettings.json` under `DatabaseConnection`.

3. **Run migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the project**
   ```bash
   dotnet run --project examScheduler
   ```

5. **Access the API** at `https://localhost:5001` and view OpenAPI documentation at `/swagger`.

## License

This project is licensed under the MIT License.