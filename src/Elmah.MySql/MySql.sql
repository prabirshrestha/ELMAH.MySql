-- ELMAH DDL script for MySql

/* ------------------------------------------------------------------------ 
        TABLES
   ------------------------------------------------------------------------ */

CREATE TABLE Elmah_Error
(
	`ErrorId`		NVARCHAR(32)		NOT NULL,
	`Application`	NVARCHAR(60)		NOT NULL,
	`Host`			NVARCHAR(50)		NOT NULL,
	`Type`			NVARCHAR(100)		NOT NULL,
	`Source`		NVARCHAR(60)		NOT NULL,
	`Message`		NVARCHAR(500)		NOT NULL,
	`User`			NVARCHAR(50)		NOT NULL,
	`StatusCode`	INT					NOT NULL,
	`TimeUtc`		DATETIME			NOT NULL,

	/* Sequence cannot be auto incremented, otherwise we get the following error.
	   Incorrect table definition; there can be only one auto column and it must be defined as a key :-(
	   triggers to the solution :-)
	*/
	`Sequence`		INT					NOT NULL, -- this will be generated via trigger


	`AllXml`		TEXT				NOT NULL,
	PRIMARY KEY(`ErrorId`)
);

-- function to get the new sequence number
DELIMITER $$

-- DROP FUNCTION IF EXISTS `Emlah_Error_NewSequenceNumber` $$
CREATE FUNCTION  `Emlah_Error_NewSequenceNumber`() RETURNS INT
    NO SQL
BEGIN
      DECLARE newSequence INT;
      SELECT IFNULL(MAX(`Sequence`) +1 , 1) INTO newSequence FROM `elmah_error`;
      RETURN(newSequence);
END $$
DELIMITER ;


-- trigger to make sure we get our sequence number in the table
-- DROP TRIGGER IF EXISTS `trgElmah_Error_AutoIncrementSequence`;
CREATE TRIGGER `trgElmah_Error_AutoIncrementSequence`
BEFORE INSERT on `Elmah_Error`
FOR EACH ROW SET NEW.`Sequence` = Emlah_Error_NewSequenceNumber();