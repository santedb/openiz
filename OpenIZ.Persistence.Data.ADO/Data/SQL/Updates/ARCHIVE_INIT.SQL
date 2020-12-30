-- TRUNCATE CODE TABLE
truncate table cd_tbl cascade;

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


delete from sec_usr_rol_assoc_tbl where usr_id in (select usr_id from sec_usr_tbl where usr_name in ('Administrator',
'Bob',
'Allison'));
delete from sec_usr_tbl where usr_name in ('Administrator',
'Bob',
'Allison');