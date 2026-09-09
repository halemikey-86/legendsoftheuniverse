-- Card front/back art (jpg, png). Files live on disk; DB stores relative paths.

alter table cards add column if not exists front_image_path text;
alter table cards add column if not exists back_image_path text;

create index if not exists cards_front_image_idx on cards (front_image_path)
  where front_image_path is not null;

comment on column cards.front_image_path is 'Relative path under catalog/uploads/, e.g. endless-01/front.jpg';
comment on column cards.back_image_path is 'Optional custom back; default playmat/card back when null';
