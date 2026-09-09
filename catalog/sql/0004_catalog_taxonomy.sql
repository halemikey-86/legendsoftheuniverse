-- Sets and series registry (powers admin dropdowns + suggestions)

create table if not exists catalog_taxonomy (
  id         bigserial primary key,
  kind       text not null check (kind in ('set', 'series')),
  name       text not null,
  code       text,
  created_at timestamptz not null default now(),
  constraint catalog_taxonomy_kind_name_uidx unique (kind, name)
);

create index if not exists catalog_taxonomy_kind_idx on catalog_taxonomy (kind);

-- Seed common values (safe to re-run)
insert into catalog_taxonomy (kind, name, code) values
  ('set', 'James The Endless', 'ENDLESS'),
  ('set', '10th Planet', 'PLANET'),
  ('set', 'Mostorno', null),
  ('set', 'Politics of Time', 'TIME'),
  ('set', 'Classic Cartoon', 'CARTOON'),
  ('set', 'Aliens', null),
  ('set', 'Goblin King', 'GK'),
  ('set', 'The Goblin King', 'GK'),
  ('set', 'History''s Finest', null),
  ('set', 'Oval Years', null),
  ('set', 'Rise of Pride', null),
  ('set', 'River Merchant', null),
  ('set', 'Scooby-Doo', null),
  ('series', 'History', null),
  ('series', 'Movies', null),
  ('series', 'Classic Cartoon', null),
  ('series', 'Video Games', null),
  ('series', 'Politics of Time', null),
  ('series', '10th Planet', null),
  ('series', 'GK', null)
on conflict (kind, name) do nothing;
