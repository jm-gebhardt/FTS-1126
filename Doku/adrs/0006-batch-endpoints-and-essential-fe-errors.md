# 6. Batch endpoints and essential fe errors

Date: 2025-05-20

## Status

Accepted

## Context

Certain batch endpoints can lead to "essential" front end errors, in case an agv has incomplete
data and for the given correlation id the endpoint returns 400. "Essential" frontend errors
imply the user can not operate the UI anymore.

## Decision

Selected endpoints can differ in their behavior if they lead to "essential" frontend errors.
These batch endpoints do not respond with 400 for requests that include a correlation id for which no data is 
found.
`POST agvs/data` and `POST agvs/graph` are returning 200 and no entry in their response list, when
no data is found for a given agv correlation id. 

## Consequences

Not all batch API endpoints behave in the same way.
