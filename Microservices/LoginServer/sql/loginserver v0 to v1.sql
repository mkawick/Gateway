CREATE SEQUENCE character_character_id_seq
    INCREMENT 1
    START 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    CACHE 1;

CREATE TABLE character_profile
(
    character_id integer NOT NULL DEFAULT nextval('character_character_id_seq'::regclass),
    name character varying(50) COLLATE pg_catalog."default" NOT NULL,
    state json,
    account_2_product_id integer NOT NULL,
    CONSTRAINT character_pkey PRIMARY KEY (character_id),
    CONSTRAINT character_profile_account_2_product_id_fkey FOREIGN KEY (account_2_product_id)
        REFERENCES public.account_2_product (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);