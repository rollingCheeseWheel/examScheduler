# examScheduler

examScheduler is an exam scheduling and management system designed to simplify the process of creating and maintaining comprehensive exam timetables for schools using [Digitales Register](https://digitalesregister.it/).
The web-interface for the application can be found [here](https://github.com/rollingCheeseWheel/examSchedulerSite).
## Architecture Overview

| Project | Description |
|----------|-------------|
| **examScheduler** | Main ASP.NET Core Web API project. |
| **Entities** | Contains all EF Core data models. |
| **Models** | DTOs used for API communication. |
| **RegisterClient** | Handles OAuth and data retrieval from <school-id>.digitalesregister.it |
| **Util** | Helper utilities and shared logic. |
| **test** | Test project for integration and unit testing. |
