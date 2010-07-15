-- ELMAH DDL script for MySql

/* ------------------------------------------------------------------------ 
        TABLES
   ------------------------------------------------------------------------ */

CREATE TABLE Elmah_Error
(
	`ErrorId`		CHAR(36)			NOT NULL,
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

-- can't put UUID() as default value,
-- mysql 5 requires default values to be constant
-- but i how do i get the last inserted guid in mysql?
-- so better send guid from the one who inserts it - the app itself (C#)
--
-- CREATE TRIGGER `trgElmah_Error_AutoGUID`
-- BEFORE INSERT ON `Elmah_Error`
-- FOR EACH ROW SET NEW.ErrorId = UUID();

/* ------------------------------------------------------------------------ 
        STORED PROCEDURES                                                      
   ------------------------------------------------------------------------ */
DELIMITER $$
CREATE PROCEDURE Elmah_GetErrorXml
(
	IN `Application` NVARCHAR(60),
	IN `ErrorId`	 CHAR(36)
)
BEGIN
	SELECT `AllXml`
	FROM `Elmah_Error`
	WHERE 
		`ErrorId` = ErrorId	AND `Application` = Application
END $$

CREATE PROCEDURE `Elmah_GetErrorsXml`
(
	IN  `Application` NVARCHAR(60)
	IN  `PageIndex`	 INT,
	IN  `PageSize`	 INT,
	OUT `TotalCount` INT
)
BEGIN
	SELECT COUNT(*) FROM `Elmah` INTO `TotalCount` WHERE `Application`=Application
	
	SET @index = PageIndex * PageSize + 1;
	SET @count = PageSize;
	PREPARE STMT FROM 'SELECT * FROM `elmah_error` WHERE `Application`=Application ORDER BY `TimeUtc` DESC, `Sequence` DESC LIMIT ?,?';
	EXECUTE STMT USING @index, @count;

END $$

DELIMITER ;