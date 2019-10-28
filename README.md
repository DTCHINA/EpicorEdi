# EpicorEdi
A C# library for parsing the text files produced by Epicor 10's EDI report styles. This project is not affiliated with or endorsed by Epicor Software Corporation.

## Background
Epicor's EDI reports use a clever self-indexing format. It's basically a CSV file using tildes as delimiters. Each file begins with one or more schema rows that identify the column names for the data rows. The first column of each data row identifies the row type. The file can contain multiple groups of related data rows, which typically correspond to business documents like invoices. It looks something like this:
```
Schema_Foo~Foo1~Foo2~Foo3
Schema_Bar~Bar1~Bar2
Schema_Baz~Baz1~Baz2~Baz3~Baz4
Foo~ABC~123~0
Bar~123~123.45
Bar~456~456.789
Foo~DEF~456~1
Baz~Apples~Oranges~42~true
```
Assuming the data rows within each document appear in the same order as the schema rows, we should be able to detect the beginning of a new document using only the row types, without any semantic knowledge of the data. For example, we don't need to know that related `InvcHead` and `InvcDtl` rows have the same `InvoiceNum`. We just keep reading until we encounter a row type that appears earlier in the schema order than the previous row type.

But there are bugs.

## Bug #1: Data Row Order
It has been reported that when an EDI report style uses a custom report data definition, the data rows within each document may appear out of schema order. If we detect the beginning of a new document based on the schema order, documents could be split in the middle. Fortunately, rows of the same type still seem to be grouped together in each document, so we can detect the beginning of a new document when the next row type differs from the previous row type and the current document already contains a row of the next row type. But this can produce incorrect results if the first or last row in a document (whether in schema order or not) is a type that may not appear in every document. Fortunately, we can detect when this might have happened and flag the file as ambiguous.

## Bug #2: Column Order
If the consumer of a file builds an index from the schema rows, then column order shouldn't matter. Unfortunately, we haven't found an EDI VAN that actually uses the schema rows. (Not even Epicor's own partner TIE.) Everyone's integrations use fixed column positions, which is a major problem because Epicor can and does change the column order on a whim.

You're supposed to be able to lock in the column order by associating an EDI definition with your report style. An EDI definition is a rectangular CSV file with table names in the first row and column names in order below each table name. The EDI definition for the example report above would look like this:
```
Foo,Bar,Baz
Foo1,Bar1,Baz1
Foo2,Bar2,Baz2
Foo3,,Baz3
,,Baz4
```
Unfortunately, using an EDI definition triggers another Epicor bug. The first row type in most of the stock reports is Company. When using an EDI definition, Epicor no longer emits the Company row for every document in the file, only the first one. If your VAN uses the Company row to identify the beginning of a new document, it will merge all your documents into one. Fortunately, we can still split the documents correctly using either the row order or the workaround for the row order bug, and then replace the missing company rows and rewrite the file before sending it to the VAN. Or we can remove the EDI definition from the report style, and use this library to rewrite the file using either an EDI definition or the schema from a known good file.

Despite these issues, using this library to read Epicor's EDI files is *vastly* more reliable than depending on a fixed column order. We hope that some VAN will pick up this library and start using it. Let us know if you do.