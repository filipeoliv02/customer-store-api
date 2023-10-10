![Rocket Store API Code Challenge](./_assets/logo-small.png "Rocket Store API Code Challenge")

Welcome to the **Rocket Store API Code Challenge**.

# Overview

The Rocket Store API is a solution, still under development, that aims to provide a back-end REST API for managing an eCommerce Web application, the Rocket Store (developed by a different team).

This solution is built with C# and .NET 5 and uses the Entity Framework to manage persistence.

Currently, it has 2 projects:

- `WebApi.csproj` - defines the back-end REST API Web application.
- `WebApiTests.csproj` - defines unit and integration tests for the REST API.

> The database is implemented in-memory only. There is no need for a real database yet (that will added at a later stage of development).

# The Challenge

The solution was started by a single developer that unfortunately had to move suddenly before it could finish the first feature. He left only a basic structure and a solution that has several issues.

First, you need to fix these errors to have the REST API up and running correctly, before adding additional features as described in the user stories below.

Good luck! :muscle:

> Feel free to ask us anything. Don't waste time if something is not clear to you. A working solution, even if incomplete, is better than something that does not compile or raises errors.

> The easiest way to get started is to open the solution in Visual Studio and start the `WebApi` project, but you are free to use the IDE of your choice.

## Definition of Done (DOD)

Considering that PRIMAVERA adopts Scrum, you will have to comply with the following DoD for each user story:

- The **acceptance criteria** is satisfied.
- **Unit and/or integration tests** are implemented and passing.
- The **Open API documentation** for each endpoint is available and complete.
- All appropriate **validations** are implemented.
- **REST API best practices** (e.g. correct status codes) are applied.
- **Code rules** are strictly respected.

# User Stories

Here are the user stories that need to be implemented for the first alpha release of the Rocket Store API. The first one is already started.

## Feature: Customers

### US01 - As a developer of the Rocket Store application, I want to be able to create a customer using the REST API.

- This action should be available from the endpoint `POST /api/customers`.
- The payload should be composed by the customer's name, email, address and VAT number. Name and email should be mandatory.
- It should not be possible to create two customers with the same email address.
- The response should contain the unique identifier of the newly created customer.

### US02 - As a developer of the Rocket Store application, I want to be able to retrieve a list of existing customers using the REST API.

- This action should be available from the endpoint `GET /api/customers`.
- The response should contain a collection with all the customers, including the identifier, name, and email address for each customer.
- The endpoint should also include an optional query parameter to allow filtering customers by name and/or email.

### US03 - As a developer of the Rocket Store application, I want to be able to retrieve a single existing customer using the REST API.

- This action should be available from the endpoint `GET /api/customers/{id}`.
- The response should contain all the information available for the customer.
- Inexistent customers should be handled appropriately.

### US04 - As a developer of the Rocket Store application, I want to be able to delete an existing customer using the REST API.

- This action should be available from the endpoint `DELETE /api/customers/{id}`.
- Inexistent customers should be handled appropriately.

### US05 - As a developer of the Rocket Store application, I want to retrieve geolocation information about an existing customer using the REST API.

- The endpoint that allows retrieving a single customer should include its address along with a collection of geolocation items.
- A geolocation item may be the longitude, the latitude, etc.
- This geolocation information should be retrieved from a third-party service.

> You may use the [PositionStack-API](https://positionstack.com/) free plan service for example.

Congratulations, that's it! You reached the end of the challenge. :+1:

## Bonus

Do you think it was easy? Well, if that's so, we have an extra task for you.

Stakeholders want to complete the whole solution faster so they want to add more developers to the team. :smile:

Additionally, the initial solution design will not be adequate as it grows to support more scenarios. The technical manager plans to implement a [Clean Architecture](https://www.youtube.com/watch?v=dK4Yb6-LxAk&t=846s) to make it more scalable and easier to mantain in the future.

How would refactor the solution to put in place the Clean Architecture design principles? Or would you purpose something different?