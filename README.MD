Verify Raw SQL for Visual Studio
===================================

Extension for checking raw sql strings in C# code.

![](https://github.com/pkochubey/VerifyRawSql/blob/master/VerifyRawSql/Resources/how-it-is-work.gif)

Verify Raw SQL is currently in public preview. We're looking forward to working with the community to improve both the rules over the next few months, and welcome your feedback and contributions!

### Installation

This extension available in the Visual Studio marketplace, use words at search VRS, Verify Raw SQL.

### Options

**Tools->Options->Verify Raw SQL** you can control:

1. When will be started checking, in case save document or\and build project.
2. What analysis will be use: Static, Dynamic or Both

For working dynamic analysis you should add new connection string to your DB with name **VRS**.

### Platform support

Operating System: Microsoft Windows 7 and later

Visual Studio: The extension requires Visual Studio 2015 and higher

Languages: C#


### Changelog

### 1.1.3 (2017-04-16)

Added new option: "Levenshtein distance" it's used for recognize typing error in first words in string

Improvement in catching interpolated string strings


### 1.1.2 (2017-04-03)

Added new option: "First Keywords" it's first list of word in the string for recognize SQL string. 


### 1.1.1 (2017-04-01)

Fixed repeated issues


### 1.1.0 (2017-03-31)

Added static type of validation

Added new option in option page

Improvement in catching SQL strings


### 1.0.0 (2017-03-28)

Added dynamic type of validation