-- TRUNCATE CODE TABLE
truncate table cd_tbl cascade;
truncate table asgn_aut_tbl cascade;
-- replace CODE CHECKS
CREATE OR REPLACE FUNCTION IS_CD_SET_MEM(
	CD_ID_IN IN UUID,
	SET_MNEMONIC_IN IN VARCHAR(32)
) RETURNS BOOLEAN AS 
$$
BEGIN
	return true;
END;
$$ LANGUAGE PLPGSQL;


CREATE OR REPLACE FUNCTION ASSRT_CD_CLS(
	CD_ID_IN IN UUID,
	CLS_MNEMONIC_IN IN VARCHAR(32)
) RETURNS BOOLEAN AS
$$
BEGIN
	return true;
END;
$$ LANGUAGE PLPGSQL;


alter table sec_usr_tbl drop constraint fk_sec_usr_crt_usr_id;
alter table sec_usr_tbl drop constraint fk_sec_usr_upd_usr_id;
alter table sec_usr_tbl drop constraint fk_sec_usr_obslt_usr_id;
alter table sec_rol_tbl drop constraint fk_sec_rol_crt_usr_id;
alter table sec_rol_tbl drop constraint fk_sec_rol_upd_usr_id;
alter table sec_rol_tbl drop constraint fk_sec_rol_obslt_usr_id;
alter table sec_pol_tbl drop constraint fk_sec_pol_crt_utc;
alter table sec_pol_tbl drop constraint fk_sec_pol_obslt_utc;

delete from sec_usr_rol_assoc_tbl where usr_id in (select usr_id from sec_usr_tbl where usr_name in ('Administrator',
'Bob',
'Allison'));
delete from sec_usr_tbl where usr_name in ('Administrator',
'Bob',
'Allison');

CREATE OR REPLACE FUNCTION trg_vrfy_ent_rel_tbl () RETURNS TRIGGER AS $$
DECLARE 
	err_ref varchar(128)[];
	
BEGIN
	RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION public.is_ent_cls(ent_id_in uuid, cls_mnemonic_in character varying)
 RETURNS boolean
 LANGUAGE plpgsql
AS $function$
BEGIN
	RETURN true;
END
$function$
;