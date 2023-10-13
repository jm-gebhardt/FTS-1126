# 2. Rest api schema

Date: 2023-10-06

## Status

Accepted

## Context

The current REST API design has problems scaling and naming is very confusing.

## Decision

We therefore add a new REST API design that will scale better

    All Single GET are available as batch GET if content type JSON
    Ids
        CorrelationId always relates to the current resource.
        All other references will have the reference name + ID: NodeId, GraphId
    Batch resources should use a filter and not return all available resources
    Naming convention
        Single resource: singular /agv
        Batch resource: plural /agvs
        The resource below will have singular name if it is singular, and plural if plural: errors → List of errors, position → single position object
        Result under plural are always lists, if resource below is single than last resource is singular. /agvs/position
            singular/singular → object
            singular/plural → object{ List[] }
            plural/singular → List[object]
            plural/plural → List[List[object]]
        Batch
            POST → Result length is known by requester
            GET → Unknown result length
            Use GET on batch if length of parameter is predictable: List of IDs not allowed
            Use POST on batch if length of parameters is not predictable: Like List of IDs
        Error in request, such as unknown ID → BadRequest 400


## Consequences

New endpoints added to the REST API need to follow this schema.
