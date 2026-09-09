-- WILLBOUND card catalog — Phase 1 schema (safe to re-run)
-- Engine B JSON schemaVersion 1.2

create table if not exists cards (
  id              text primary key,
  schema_version  text not null default '1.2',
  set_name        text not null default '',
  number          text not null default '',
  series          text not null default '',
  name            text not null,
  type            text not null,
  subtype         text not null default '',
  role            text,
  frame           text not null default 'standard',
  will_cost       integer not null default 0,
  store_worth     integer not null default 0,
  honor_cost      integer not null default 0,
  honor_gain      integer not null default 0,
  strike          integer not null default 0,
  guard           integer not null default 0,
  health          integer not null default 0,
  keywords        jsonb not null default '[]'::jsonb,
  hunted          integer not null default 0,
  aftereffect     jsonb,
  mend            integer not null default 0,
  doubleteam      integer not null default 0,
  starts_in_play  boolean not null default false,
  abilities       jsonb not null default '[]'::jsonb,
  spells          jsonb not null default '[]'::jsonb,
  flavor          text not null default '',
  art_prompt      text not null default '',
  notes           text not null default '',
  created_at      timestamptz not null default now(),
  updated_at      timestamptz not null default now(),
  constraint cards_type_check check (
    type in ('Icon', 'Companion', 'Relic', 'Bond', 'Surge', 'Will')
  ),
  constraint cards_frame_check check (
    frame in ('standard', 'gold', 'universe')
  ),
  constraint cards_will_cost_check check (will_cost between 0 and 8),
  constraint cards_store_worth_check check (store_worth between 0 and 20)
);

-- No duplicate card number within a set (e.g. two "05/50" in James The Endless)
create unique index if not exists cards_set_number_uidx
  on cards (set_name, number)
  where number <> '';

create index if not exists cards_name_idx on cards (lower(name));
create index if not exists cards_type_idx on cards (type);
create index if not exists cards_set_idx on cards (set_name);
create index if not exists cards_series_idx on cards (series);
create index if not exists cards_frame_idx on cards (frame);
create index if not exists cards_will_cost_idx on cards (will_cost);

-- Audit trail for admin changes (Phase 2+)
create table if not exists card_audit (
  id            bigserial primary key,
  card_id       text not null,
  action        text not null,
  actor         text not null default 'system',
  payload       jsonb,
  created_at    timestamptz not null default now()
);

create index if not exists card_audit_card_id_idx on card_audit (card_id);
