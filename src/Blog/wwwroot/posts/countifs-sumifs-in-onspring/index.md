```json meta
{
  "title": "CountIfs and SumIfs in Onspring",
  "lead": "How to use multiple criteria to count and sum records in Onspring",
  "isPublished": true,
  "publishedAt": "2023-10-21",
  "openGraphImage": "posts/countifs-sumifs-in-onspring/og-image.png",
}
```

 | [![thumbnail](posts/countifs-sumifs-in-onspring/thumbnail.png)](https://www.youtube.com/watch?v=CfpsO7_Emd0) |
 |:--:|
 |*Click above to watch a video demonstration*|

If you have spent a lot of time working in spreadsheets there is likely little chance that you haven't heard of the [countifs](https://support.microsoft.com/en-us/office/countifs-function-dda3dc6e-f74e-4aee-88bc-aa8c2a866842) or [sumifs](https://support.microsoft.com/en-us/office/sumifs-function-c9e748f5-7ea7-455d-9406-611cebce642b) function and a far more likely chance you've actually used both.

And for good reason too.

These functions are super simple, but really powerful because you can use them to quickly filter your data and calculate some basic metrics that allow you to better understand a large data set.

And you might have already guessed, but the same holds true for these sorts of calculations in [Onspring](https://onspring.com/) as well.

For example let's say that I have a super simple projects app and each project in my app can have many related task records. Something like this:

![projects-app-example](posts/countifs-sumifs-in-onspring/projects-app-example.png)

When the number of tasks related to the project is small it is pretty easy to just pop open the content record and get an idea of what is going on with the project, but as the number of related records grow it would probably be helpful to have some metrics that quickly summarize the state of things.

Perhaps some simple counts that tell us...

- How many total tasks there are
- How many of those tasks are complete
- How many of those tasks are in progress
- How many of those tasks are of a particular type

Something like this:

![basic-formula-example](posts/countifs-sumifs-in-onspring/basic-formula-examples.png)

Which is pretty straightforward to do because each of those counts are based upon no more than a single criteria and [Onspring](https://onspring.com/) already gives us a really handy CountIf function to work with. Below is an example of the CountIf function at work for counting the number of complete tasks.

```javascript
CountIf({:Tasks::Status}==[:Complete], {:item::Record Id});
```

But what if I want to continue building these task counts and subdiving them not just by their status or type, but by both. Meaning I want to create my counts using multiple criteria. This is when you'd most likely default to reaching for something like [countifs](https://support.microsoft.com/en-us/office/countifs-function-dda3dc6e-f74e-4aee-88bc-aa8c2a866842) or [sumifs](https://support.microsoft.com/en-us/office/sumifs-function-c9e748f5-7ea7-455d-9406-611cebce642b).

However in Onspring there are not built-in countifs and sumifs functions and if you look at the documentation for the CountIf or SumIf functions you'll see they only accept a single criteria parameter. For example:

> [!NOTE]
> The CountIf function can evaluate only one criterion at a time.

This might be where an admin isn't sure what to do next and it's where I'd like to help. Turns out this problem is completely solvable and the answer really isn't all that complex. It just requires looking at things from a different perspective.

Let's start by introducing a formula field at our task records that will allow each individual task record to identify itself as having met the criteria or not to be included in a particular count at the project record.

Take for example that I want to have a count at the project record of all the incidental tasks that are in progress. So let's add a formula field at the task record which indicates whether the task is exactly that - incidental and in progress.

Here is an example of that field and it's formula syntax:

![helper-formula-field-example](posts/countifs-sumifs-in-onspring/helper-formula-field-example.gif)

```javascript
if ({:Type}==[:Incidental] && {:Status}==[:In Progress]) return [:True];
return [:False];
```

With that in place at our task records our project records will be able to use that field to identify which related task records have met a particular criteria and therefore should be included as part of a particular count.

![task-records-from-project-record-example](posts/countifs-sumifs-in-onspring/task-records-from-project-record-example.png)

And here is an example of implementing a count of all in progress incidental tasks at the project record.

![multi-criteria-count-example](posts/countifs-sumifs-in-onspring/multi-criteria-count-example.png)

```javascript
CountIf({:Tasks::IsIncidentalAndIsInProgress}==[:True], {:item::Record Id});
```

This type of pattern can then be repeated as many times as you need to get at all the different sums and counts you need. You can also use as complex a criteria as you want to identify the records to include or exclude from your calculations.

I hope this is helpful and makes your job as an admin just a little easier.
