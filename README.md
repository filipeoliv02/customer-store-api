# Overview

The Customer Store API is a solution that aims to provide a back-end REST API for managing an eCommerce Web application. It was built in [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

This solution is built with C# and .NET and uses the Entity Framework to manage persistence.

Currently, it has 2 projects:

- `WebApi.csproj` - defines the back-end REST API Web application.
- `WebApiTests.csproj` - defines unit and integration tests for the REST API.

> The database is implemented in-memory only. There is no need for a real database yet (that will added at a later stage of development).

# User Stories

## Feature: Customers

### US01 - As a developer of the Customer Store application, I want to be able to create a customer using the REST API.

- This action should be available from the endpoint `POST /api/customers`.
- The payload should be composed by the customer's name, email, address and VAT number. Name and email should be mandatory.
- It should not be possible to create two customers with the same email address.
- The response should contain the unique identifier of the newly created customer.

### US02 - As a developer of the Customer Store application, I want to be able to retrieve a list of existing customers using the REST API.

- This action should be available from the endpoint `GET /api/customers`.
- The response should contain a collection with all the customers, including the identifier, name, and email address for each customer.
- The endpoint should also include an optional query parameter to allow filtering customers by name and/or email.

### US03 - As a developer of the Customer Store application, I want to be able to retrieve a single existing customer using the REST API.

- This action should be available from the endpoint `GET /api/customers/{id}`.
- The response should contain all the information available for the customer.
- Inexistent customers should be handled appropriately.

### US04 - As a developer of the Customer Store application, I want to be able to delete an existing customer using the REST API.

- This action should be available from the endpoint `DELETE /api/customers/{id}`.
- Inexistent customers should be handled appropriately.

### US05 - As a developer of the Customer Store application, I want to retrieve geolocation information about an existing customer using the REST API.

- The endpoint that allows retrieving a single customer should include its address along with a collection of geolocation items.
- A geolocation item may be the longitude, the latitude, etc.
- This geolocation information should be retrieved from a third-party service.

> [PositionStack-API](https://positionstack.com/) free plan service should be used as example.

``` An environment variable should be set in order to use the geolocation service. PositionStackApiKey : "valid_key" ```