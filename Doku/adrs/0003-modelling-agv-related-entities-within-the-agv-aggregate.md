# 3. Modelling Agv Related Entities Within the AGV Aggregate

Date: 2023-11-07

## Status

Accepted

## Context

In our domain model, there are multiple entities that have a relationship with the AGV.
The design decision to be made is whether these entities should be managed within the AGV aggregate, adhering to domain-driven design (DDD) principles, or treated as separate entities.
This would enable us to perform actions related to the domain such as agv.AddCargo(cargo) according to the DDD.
To ensure the consistency of the Agv aggregate, we use a partitioner that ensures that commands are handled serially by the AgvCommandHandler, which is the only place that has write access to the Agv entity.

Implications if one would decide to stick it to the Agv aggregate:
We would need to extend the AgvRepository with at least the write method for Cargo.
The Agv Aggregate would continue to grow (currently 451 lines).
The partitioner would be further overloaded, as even more telegrams would have to be added.
Since most entities are assigned to the Agv, since everything revolves around it, the Agv would explode.

## Decision

Guiding principles:
- Do not add new entities to the AGV aggregate when:
  - The entity has high-frequency updates that are seldom required
  - The entity's data is quite big
- No unnecessary intermediate aggregates should be introduced. If something actually relates to the Agv Aggregate, then it stays as an isolated entity.
- When such an entity is not part of the Agv Aggregate:
  - It has it's own ClassMap
  - The relation to the Agv and the foreign key to the Agv table is defined in the ClassMap

## Consequences

- **Reduced Aggregate Complexity**: Avoiding the inclusion of entities with frequent updates or large data volumes prevents the AGV aggregate from becoming overly complex and difficult to maintain.
- **Improved Performance**: Handling entities outside the AGV aggregate can lead to more efficient processing, especially for those with high update frequencies that are not often needed in the context of the AGV.
- **Partitioner Load**: The partitioner's load may be reduced as fewer commands need to be handled serially.
