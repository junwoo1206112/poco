## ADDED Requirements

### Requirement: Rainbow block subtype

The system SHALL support Rainbow as a new `PokoBlockSubtype` value alongside Frozen, Stone, and Clock.

#### Scenario: Rainbow tile is created

- **WHEN** a tile is assigned the Rainbow subtype
- **THEN** its `BlockSubtype` is `PokoBlockSubtype.Rainbow`, it is linkable, and renders with a rainbow gradient pattern

#### Scenario: Rainbow tile is linkable

- **WHEN** a Rainbow tile exists on the board
- **THEN** `IsLinkable` returns true for the tile (same as Clock)

#### Scenario: Rainbow tile spawn probability

- **WHEN** the board generates or refills
- **THEN** Rainbow tiles have a ~6% chance of appearing via `RandomSubtype()`, the same rate as other special blocks
