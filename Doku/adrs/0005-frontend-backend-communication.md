# 5. Frontend-Backend Communication

Date: 2024-02-06

## Status

Accepted

## Context
The frontend backend communication can be divided into three parts.
(a) Static query data frontend-to-backend
(b) Sending asynchronous events / changes from server-to-frontend
(c) Send Action/Update data from frontend-to-server

## Decision
HTTP Requests: Used for all (a) initial, static requests as well as (c) updating/creating data 
WebSockets/SignalR: Only used for (b) sending updates from backend to frontend in the form of events. No real bi-directional communication is used, the frontend will only subscribe to hubs.
  
### HTTP-Requests
Static HTTP API are designed as a REST-API.
The REST API is documented using OpenAPI v3.

#### Schema
The REST-API Schema is specified in ADR-0002.

### WebSockets / SignalR
SignalR is used to send asynchronous data to client-side.
The hubs and messages are documented using OpenAPI v3. 
No real bi-directional communication is used:
 * Clients connects to the hub
 * Server calls methods on all clients connected on specific hub

#### Schema
SignalR methods have the nature of event handlers.
Methods names are combined using the Entity that has been changed, suffixed by a verb indicating the event: Change, Created, Removed.

* All SignalR message include a `messageCreateDate: date-time` to indicate the creating of the message in an async environment.
* Remove events only include the removed Id and no further content.

### Data Models
SignalR and REST-API should use the same models when possible.

## Consequences

