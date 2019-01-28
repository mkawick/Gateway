
--
-- TOC entry 188 (class 1259 OID 16427)
-- Name: account_user_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE account_user_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


SET default_with_oids = false;

--
-- TOC entry 186 (class 1259 OID 16409)
-- Name: account; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE account (
    user_id integer DEFAULT nextval('account_user_id_seq'::regclass) NOT NULL,
    username character varying(50) NOT NULL,
    password character varying(50) NOT NULL,
    email character varying(355) NOT NULL,
    created_on timestamp without time zone,
    last_login timestamp without time zone
);


--
-- TOC entry 189 (class 1259 OID 16429)
-- Name: account_2_product_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE account_2_product_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 191 (class 1259 OID 16435)
-- Name: account_2_product; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE account_2_product (
    id integer DEFAULT nextval('account_2_product_id_seq'::regclass) NOT NULL,
    user_id integer NOT NULL,
    product_id integer NOT NULL,
    created_on timestamp without time zone
);


--
-- TOC entry 190 (class 1259 OID 16431)
-- Name: product_product_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE product_product_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 187 (class 1259 OID 16418)
-- Name: product; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE product (
    product_id integer DEFAULT nextval('product_product_id_seq'::regclass) NOT NULL,
    productname character varying(50) NOT NULL,
    created_on timestamp without time zone DEFAULT now()
);

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

--
-- TOC entry 2147 (class 0 OID 16409)
-- Dependencies: 186
-- Data for Name: account; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO account VALUES (2, 'mickey', 'password', 'mickey.kawick@jagex.com', NULL, NULL);


--
-- TOC entry 2152 (class 0 OID 16435)
-- Dependencies: 191
-- Data for Name: account_2_product; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO account_2_product VALUES (1, 2, 2, NULL);
INSERT INTO account_2_product VALUES (2, 2, 1, NULL);


--
-- TOC entry 2148 (class 0 OID 16418)
-- Dependencies: 187
-- Data for Name: product; Type: TABLE DATA; Schema: public; Owner: -
--

INSERT INTO product VALUES (1, 'hungry hippos', '2018-01-10 14:39:52.300375');
INSERT INTO product VALUES (2, 'micro machines', '2018-01-10 14:39:52.300375');


--
-- TOC entry 2162 (class 0 OID 0)
-- Dependencies: 189
-- Name: account_2_product_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('account_2_product_id_seq', 2, true);


--
-- TOC entry 2163 (class 0 OID 0)
-- Dependencies: 188
-- Name: account_user_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('account_user_id_seq', 2, true);


--
-- TOC entry 2164 (class 0 OID 0)
-- Dependencies: 190
-- Name: product_product_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('product_product_id_seq', 3, true);


--
-- TOC entry 2029 (class 2606 OID 16439)
-- Name: account_2_product account_2_product_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY account_2_product
    ADD CONSTRAINT account_2_product_pkey PRIMARY KEY (id);


--
-- TOC entry 2019 (class 2606 OID 16415)
-- Name: account account_email_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY account
    ADD CONSTRAINT account_email_key UNIQUE (email);


--
-- TOC entry 2021 (class 2606 OID 16413)
-- Name: account account_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY account
    ADD CONSTRAINT account_pkey PRIMARY KEY (user_id);


--
-- TOC entry 2023 (class 2606 OID 16417)
-- Name: account account_username_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY account
    ADD CONSTRAINT account_username_key UNIQUE (username);


--
-- TOC entry 2025 (class 2606 OID 16422)
-- Name: product product_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY product
    ADD CONSTRAINT product_pkey PRIMARY KEY (product_id);


--
-- TOC entry 2027 (class 2606 OID 16424)
-- Name: product product_productname_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY product
    ADD CONSTRAINT product_productname_key UNIQUE (productname);


-- Completed on 2018-01-11 14:29:09

--
-- PostgreSQL database dump complete
--

