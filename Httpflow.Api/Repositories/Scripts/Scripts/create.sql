CREATE TABLE IF NOT EXISTS `Users` (
    `Id` INT PRIMARY KEY AUTO_INCREMENT,
    `Firstname` VARCHAR(255) NOT NULL,
    `Lastname` VARCHAR(255) NOT NULL,
    `Email` VARCHAR(320) NOT NULL UNIQUE,
    `Password` VARCHAR(255) NOT NULL
);

CREATE TABLE IF NOT EXISTS `Projects` (
    `Id` INT PRIMARY KEY AUTO_INCREMENT,
    `OwnerUserId` INT NOT NULL,
    `Name` VARCHAR(255) NOT NULL,
    `Value` LONGTEXT NOT NULL,
    CONSTRAINT `FK_Projects_Users`
        FOREIGN KEY (`OwnerUserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `UQ_Projects_OwnerUserId_Name`
        UNIQUE (`OwnerUserId`, `Name`)
);

CREATE TABLE IF NOT EXISTS `Collections` (
    `Id` INT PRIMARY KEY AUTO_INCREMENT,
    `ProjectId` INT NOT NULL,
    `Name` VARCHAR(255) NOT NULL,
    `Value` LONGTEXT NOT NULL,
    CONSTRAINT `FK_Collections_Projects`
        FOREIGN KEY (`ProjectId`) REFERENCES `Projects` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `UQ_Collections_ProjectId_Name`
        UNIQUE (`ProjectId`, `Name`)
);

CREATE TABLE IF NOT EXISTS `Variables` (
    `Id` INT PRIMARY KEY AUTO_INCREMENT,
    `CollectionId` INT NOT NULL,
    `Name` VARCHAR(255) NOT NULL,
    `Value` LONGTEXT NOT NULL,
    CONSTRAINT `FK_Variables_Collections`
        FOREIGN KEY (`CollectionId`) REFERENCES `Collections` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `UQ_Variables_CollectionId_Name`
        UNIQUE (`CollectionId`, `Name`)
);

CREATE TABLE IF NOT EXISTS `Commands` (
    `Id` INT PRIMARY KEY AUTO_INCREMENT,
    `ProjectId` INT NOT NULL,
    `Name` VARCHAR(255) NOT NULL,
    `Value` LONGTEXT NOT NULL,
    CONSTRAINT `FK_Commands_Projects`
        FOREIGN KEY (`ProjectId`) REFERENCES `Projects` (`Id`) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS `ProjectTeammates` (
    `ProjectId` INT NOT NULL,
    `UserId` INT NOT NULL,
    PRIMARY KEY (`ProjectId`, `UserId`),
    CONSTRAINT `FK_ProjectTeammates_Projects`
        FOREIGN KEY (`ProjectId`) REFERENCES `Projects` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ProjectTeammates_Users`
        FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
);
