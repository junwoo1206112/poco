## MODIFIED Requirements

### Requirement: Special block subtypes

The system SHALL support Frozen, Stone, and Clock as `PokoBlockSubtype` values. Rainbow SHALL NOT be a regular block subtype.

#### Scenario: Special subtype is created

- **WHEN** a tile is assigned Frozen, Stone, or Clock
- **THEN** its `BlockSubtype` reflects that special state and its tile behavior follows the subtype rules

#### Scenario: Rainbow is not a regular subtype

- **WHEN** a rainbow effect is created
- **THEN** it is represented as a `BombType.Rainbow` bomb generated from the rainbow gauge, not as `PokoBlockSubtype.Rainbow`
