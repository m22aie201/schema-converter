CREATE DATABASE SchemaConverter;
GO

USE SchemaConverter
GO

CREATE TABLE Publishers (
    PublisherID INT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Address VARCHAR(255),
    Phone VARCHAR(20)
);

CREATE TABLE Authors (
    AuthorID INT PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL
);

CREATE TABLE Books (
    BookID INT PRIMARY KEY,
    Title VARCHAR(100) NOT NULL,
    Genre VARCHAR(50),
    PublishedYear INT,
    PublisherID INT,
    FOREIGN KEY (PublisherID) REFERENCES Publishers(PublisherID)
);

CREATE TABLE BookAuthors (
    BookID INT,
    AuthorID INT,
    PRIMARY KEY (BookID, AuthorID),
    FOREIGN KEY (BookID) REFERENCES Books(BookID),
    FOREIGN KEY (AuthorID) REFERENCES Authors(AuthorID)
);

CREATE TABLE Borrowers (
    BorrowerID INT PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Address VARCHAR(255),
    Phone VARCHAR(20)
);

CREATE TABLE BorrowRecords (
    RecordID INT PRIMARY KEY,
    BookID INT,
    BorrowerID INT,
    BorrowDate DATE,
    ReturnDate DATE,
    FOREIGN KEY (BookID) REFERENCES Books(BookID),
    FOREIGN KEY (BorrowerID) REFERENCES Borrowers(BorrowerID)
);

INSERT INTO Publishers (PublisherID, Name, Address, Phone) VALUES
(1, 'Penguin Random House', '1745 Broadway, New York, NY 10019', '212-782-9000'),
(2, 'HarperCollins', '195 Broadway, New York, NY 10007', '212-207-7000');

INSERT INTO Authors (AuthorID, FirstName, LastName) VALUES
(1, 'George', 'Orwell'),
(2, 'Jane', 'Austen'),
(3, 'J.K.', 'Rowling');

INSERT INTO Books (BookID, Title, Genre, PublishedYear, PublisherID) VALUES
(1, '1984', 'Dystopian', 1949, 1),
(2, 'Pride and Prejudice', 'Romance', 1813, 2),
(3, 'Harry Potter and the Sorcerer''s Stone', 'Fantasy', 1997, 1);

INSERT INTO BookAuthors (BookID, AuthorID) VALUES
(1, 1),
(2, 2),
(3, 3);

INSERT INTO Borrowers (BorrowerID, FirstName, LastName, Address, Phone) VALUES
(1, 'Alice', 'Smith', '123 Maple St, Anytown, USA', '555-1234'),
(2, 'Bob', 'Johnson', '456 Oak St, Sometown, USA', '555-5678');

INSERT INTO BorrowRecords (RecordID, BookID, BorrowerID, BorrowDate, ReturnDate) VALUES
(1, 1, 1, '2023-10-01', '2023-10-15'),
(2, 2, 2, '2023-10-03', '2023-10-17'),
(3, 3, 1, '2023-10-05', NULL);  -- NULL for ReturnDate indicates the book is not yet returned
