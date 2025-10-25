# examScheduler

examScheduler is a .NET 9.0 exam scheduling and management system designed to simplify the process of creating and maintaining comprehensive exam timetables for schools using [Digitales Register](https://digitalesregister.it/). It combines automation, data integrity, and flexibility with a modular architecture built on modern .NET technologies.
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
