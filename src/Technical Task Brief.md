

<!-- Start of picture text -->
JIGSAW<br><!-- End of picture text -->

# **TECHNICAL TASK** 

DEALER DATA CONSOLIDATION 

.NET SOFTWARE ENGINEER 

|**Section**|**Page**|
|---|---|
|**Introduction**|**3**|
|**Background**|**3**|
|**The Data**|**4**|
|**What We'd Like You to Build**|**4**|
|Import and matching (console application)|4|
|Data model|5|
|Your decisions|5|
|Web interface (optional)|5|
|**Technical Requirements**|**6**|
|**Starter Solution**|**6**|
|**Time and Scope**|**7**|
|**Use of AI Tools**|**7**|
|**Submission**|**7**|
|**What Happens Next**|**8**|
|What we look at|8|



Jigsaw Finance Limited 

2 

## **Introduction** 

Thank you for your interest in the .NET Software Engineer role at Jigsaw Finance. This task is a small-scale version of a real project you would work on if you join us, so it should also give you a feel for the kind of problems we solve. 

Please read this document in full before you start. 

## **Background** 

Jigsaw Finance is a UK vehicle finance broker. A growing share of our business is introduced by motor dealerships, so we are building a Dealer Database: a single "source of truth" for every dealership we might work with. 

The difficulty is that information about a dealership is spread across many sources: regulators, registries, commercial data providers and data crawled from the web. Each source uses its own format, its own identifiers and its own idea of what the dealer is called. No single field reliably links them all together. 

Your job is to take a sample of these sources, work out which records refer to the same real-world dealership, and bring them together into one clean, consolidated view. 

Jigsaw Finance Limited 

3 

## **The Data** 

The `data/` folder in the starter solution contains exports from seven sources. All data is fictional. 

|**File**|**Format**|**Description**|
|---|---|---|
|`marketcheck_dealers.csv`|CSV|Dealer listings from a stock/marketplace provider: trading<br>name, location, contact details and stock statistics|
|`companies_house.json`|JSON|Company records: legal name, company number, registered<br>office, status, incorporation/dissolution dates and officers|
|`fca_register.json`|JSON|FCA Financial Services Register extract: firm reference<br>numbers, names, trading names and authorisation status|
|`ico_register.csv`|CSV|ICO data protection fee register: registration numbers,<br>organisation names, addresses and expiry dates|
|`saf_members.xml`|XML|SAF membership register: member names, status and expiry|
|`vat_lookups/`|JSON (one file<br>per lookup)|Results of VAT number validation lookups|
|`crawled_dealers.csv`|CSV|Data scraped from dealership websites. Wide, inconsistent<br>and noisy|



Working out the structure of each file is part of the task, so we have not documented every field. Expect the data to behave like real-world data: inconsistent formatting, duplicates, missing values, conflicting information and records that look alike but are not. 

## **What We'd Like You to Build** 

A starter solution is provided to help you get going (see Starter Solution below), but you do not have to use it. 

### **Import and matching (console application)** 

A console application that, when run: 

**1.** Reads every source in the `data/` folder. 

**2.** Cleans and normalises the data (names, addresses, postcodes, phone numbers, dates, domains and so on). 

**3.** Matches records across sources to identify each distinct real-world dealership. 

**4.** De-duplicates and resolves conflicts where sources disagree. 

**5.** Stores the consolidated result in a SQLite database using Entity Framework Core (Code First, with migrations). 

The import should be safe to run more than once without creating duplicate data. 

Jigsaw Finance Limited 

4 

### **Data model** 

How you model and store the data is up to you, and we are interested in your reasoning. At a minimum, we would expect to be able to see the following for each dealership, where the sources provide it: 

- Trading name(s) and legal company name 

- Company registration number, incorporation date and company status 

- Registered and trading address(es), including postcode 

- Contact details: phone, email, website 

- FCA reference number and status 

- ICO registration number and expiry 

- SAF status and expiry 

- VAT number and validation status 

- Any other useful information unique to a source (for example stock figures, directors, or finance calculator details) 

It should be possible to tell which source(s) a piece of information came from. 

### **Your decisions** 

Please include a short `DECISIONS.md` in the root of your solution covering: 

- Your approach to matching, and how you decide two records are the same dealer 

- How you resolve conflicts between sources (which source "wins", and why) 

- Any assumptions you made about the data 

- Anything you chose not to do, and what you would do next with more time 

Bullet points are fine. We read this before we read your code. 

### **Web interface (optional)** 

Only if you have time left. This is not required, and we would rather see strong import and matching logic than a web interface. 

If you do build one, a simple, read-only ASP.NET Core MVC interface is enough, for example a searchable dealer list and a detail page showing everything known about a dealer. It does not need to look polished. 

Jigsaw Finance Limited 

5 

## **Technical Requirements** 

- .NET 8 or higher 

- Entity Framework Core (Code First, with migrations) and SQLite 

- ASP.NET Core MVC, if you build the optional web interface 

- Any NuGet packages you like, but be prepared to explain your choices 

- The solution must build and run from a clean clone, following the instructions in your README 

## **Starter Solution** 

The starter solution is there as a guide, should you want to use it. If you would prefer to use something completely different, or set your solution up in a different way, that is fine, as long as it meets the technical requirements above. 

It targets .NET 8 and contains three projects: 

|**Project**|**Description**|
|---|---|
|`DealerDatabase.Data`|Class library containing the EF Core DbContext, a minimal Dealer entity and<br>an initial migration, using SQLite|
|`DealerDatabase.Import`|Console application for the import. It currently applies migrations and lists<br>the files in`data/`|
|`DealerDatabase.Web`|ASP.NET Core MVC application for the optional web interface. It currently<br>lists the dealers in the database|



The source data is in the `data/` folder in the root of the solution. Both applications share one SQLite database, `dealers.db` , which is created in the solution root, and both apply any outstanding migrations when they start. 

To run it, from the solution folder: 

- Run the import: `dotnet run --project src/DealerDatabase.Import` 

- Run the web interface: `dotnet run --project src/DealerDatabase.Web` 

- Add a migration: run `dotnet tool restore` once, then `dotnet ef migrations add <Name> --project src/DealerDatabase.Data` 

The README in the starter solution has more detail. 

Jigsaw Finance Limited 

6 

## **Time and Scope** 

- There is no time limit. Please spend as much time as you think is necessary to complete the task, but we would advise against spending too long on additional features rather than focusing on the core requirements. 

- We do not expect it to be finished. It is deliberately broad. Prioritise what you think matters most, and use `DECISIONS.md` to tell us what you would do next. 

- The quality of your matching logic and your reasoning matter more to us than the number of features. 

## **Use of AI Tools** 

You may use AI assistants (for example Copilot, ChatGPT or Claude) as a tool, in the same way you might use them day to day at work. 

However, the solution must be your own: you should control the design and architecture, and you must be able to explain every line you submit. We may ask you about any part of your code in the follow-up interview, so please make sure you understand everything in your submission. 

## **Submission** 

- Your solution must be in a GitHub repository that we can view (a public repository is simplest). 

- Email the link to the repository to sam.ainsworth@jigsawfinance.com. 

- We cannot accept zip files or other attachments. 

- Include a `README.md` explaining how to build and run the import (and the web interface, if you built one). 

Jigsaw Finance Limited 

7 

## **What Happens Next** 

We will review your submission, starting with `DECISIONS.md` , then your code. If we like what we see, we will invite you to a follow-up call. This will be an interview, and we may ask you about your solution and the decisions you made as part of it. 

### **What we look at** 

- Correctness of matching: how accurately records are linked to the right dealer 

- Design: structure, separation of concerns and use of SOLID principles 

- Data modelling: how sensibly the consolidated data is stored 

- Code quality: readability, naming, error handling and use of async where appropriate 

- Communication: the clarity of your `DECISIONS.md` and README 

If anything in this brief is unclear, make a reasonable assumption and note it in `DECISIONS.md` . Asking us is also fine. 

Good luck, and thank you for your time. 

Jigsaw Finance – Engineering Team 

Jigsaw Finance Limited 

8 

