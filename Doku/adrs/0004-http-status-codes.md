# 4. Http status codes

Date: 2023-11-22

## Status

Accepted

## Context

The decision on HTTP status codes is guided by the principle of semantic accuracy in API responses.
It addresses the need to correctly differentiate between situations like resource existence and the availability of specific data, ensuring that each response precisely reflects the nature of the request outcome.

## Decision

We have agreed to use the following principles for HTTP status codes in the API:

    200 OK will be returned for successful requests where the expected data is provided. This can also be an empty list.
    204 No Content will be returned when a request is successful, but the result is non-existent or null.
    400 Bad Request will indicate client-side errors, such as invalid request parameters.
    404 Not Found will be used when the requested resource does not exist, implying that the URL is incorrect.
    409 Conflict will be used when e.g. the charger is currently unavailable, because it is already in use by another Agv.

## Consequences

Implementing these specific HTTP status codes will lead to clearer and more precise API responses, which is more straightforward by signaling the nature of issues, such as incorrect URLs.

## Changes
2024-03-08: Added 409 Conflict status code