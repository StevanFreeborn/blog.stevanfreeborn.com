```json meta
{
  "title": "Onspring's RefToArray Formula Function Explained: The What, Why, and How",
  "lead": "Learn how to use the RefToArray function in the Onspring formula engine to transform and aggregate data from related records. This guide breaks down how RefToArray works, offers practical use cases, and explains when to leverage it for custom data evaluation. Whether you're handling risk scores, survey responses, or task deadlines, get tips on harnessing RefToArray for data insight and control without giving yourself a headache",
  "isPublished": true,
  "publishedAt": "2024-11-11",
  "openGraphImage": "posts/onspring-reftoarray-explained/og-image.png",
}
```

I've been writing formulas in Onspring since 2020 and I've become quite taken with its formula engine. It's a powerful tool that allows you to manipulate data in all sorts of ways with as little or as much complexity as you like. Mainly because a lot of work went in to providing a bunch of built-for-you functions that make getting started a breeze, but yet because it is basically just extending JavaScript you get all the expressiveness of a full programming language. It's a great balance and allows you to take what you need and leave what you don't.

> [!NOTE]
> Onspring's formula engine is based on JavaScript, but its current implementation at the time of this writing uses an older version of the language. This means that some newer features and syntax may not work.

However one built-in function that had me a little twisted up when I first started was `RefToArray`. It's signature is one of the more complex of all the built-in functions and on its face the value it returns doesn't seem to be all that useful on its own. I remember sitting in on a brief training session on it and while I left feeling better about it there was still a lot of time between then and when I really felt like I had a solid grasp on it. So I thought I'd write the explainer that I wish I had when I was first learning how to write formulas in Onspring.

## What's the Deal with RefToArray?

The first time you see `RefToArray` in a formula it can be a little intimidating especially if for the most part you've been working with simpler one-liners. It's likely you first came across it in something like this:

```javascript
var tasks = RefToArray({:Tasks}, { status: {:item::Status}, dueDate: {:item::Due Date} });

tasks = tasks.filter(function(task){
  return task.status !== "Completed";
});

tasks = tasks.sort(function(a,b){
  return a.dueDate - b.dueDate;
});

return Object(tasks[0]).dueDate;
```

You probably saw it and thought "what the heck is that?" and then promptly moved on to something else. But if you're reading this you're probably at the point where you've run into a use case that requires it or you're having to refactor a formula that someone else wrote that uses it. So understanding what it does and how it works would be helpful.

## A Tale of Three Concepts

In order to make sense of `RefToArray` we need to understand three concepts: reference fields, objects, and arrays. The former is specific to Onspring while the latter two are related to JavaScript and programming in general. Let's take a look at each of these concepts in turn.

### Reference Fields

Reference fields are a way to link records in Onspring. They allow you to create relationships between records in different apps or surveys or within the same app or survey. For example, you might have a Tasks app and a Projects app. You could create a reference field in the Tasks app that targets the Tasks app. This would allow you to link tasks to specific projects. Establishing relationships between records in this way unlocks a lof of power in Onspring and is at the core of how you build solutions in the platform. Here is an example of what it looks like to have some tasks records that are linked to a project record:

![Reference Fields](posts/onspring-reftoarray-explained/reference-field-example.png)

### Objects

Objects are a fundamental data structure in programming. They are a way to store a collection of key-value pairs. You can think of them as a dictionary or a map. For example, you might have an object that represents a person with properties for `name`, `age`, and `gender`. Objects are useful because they allow you to group related data together and then access that data using the properties. You can add properties to an object, remove properties from an object, update the values of properties in an object, and so on. It is important to note that the values in an object can be more complex data structures like other objects or arrays. Here's an example of an object in JavaScript:

```javascript
var person = {
  name: "Stevan",
  age: 31,
};
```

In this example, `person` is an object with two properties: `name` and `age`. The value of the `name` key is `"Stevan"` and the value of the `age` key is `31`. We can access and manipulate the data in the object like this:

```javascript
// Access the value of the name key
return person.name;
```

```javascript
// Update the value of the age key
person.age = 32;
return person.age;
```

There is no way I could tell you everything you need to know about objects in a single blog post, but I hope this gives you a basic understanding of what they are and how they work. If you're interested in learning more, I recommend checking out the [MDN Web Docs](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Working_with_Objects). This should thought give you enough of an understanding to follow how they are used in the context of `RefToArray`.

### Arrays

Arrays are a fundamental data structure in programming. They are a way to store a collection of values. You can think of them as a list of items. For example, you might have an array of numbers `[1, 2, 3, 4, 5]` or an array of strings `["apple", "banana", "cherry"]`. Arrays can also contain more complex data structures like objects or other arrays. For example you could have an array of objects that represent people:

```javascript
var people = [
  { name: "Stevan", age: 32 },
  { name: "Kelsey", age: 33 },
];
```

Arrays are useful because they allow you to group related data together and then manipulate that data in various ways. You can add items to an array, remove items from an array, sort an array, and access items in an array by their index. Here's an example of how you might access the first person in the `people` array:

```javascript
// Access the first person in the array
// This will return { name: "Stevan", age: 32 }
return JSON.stringify(people[0]);
```

You can also access the properties of the object like this:

```javascript
// Access the name property of the first person in the array
// This will return "Stevan"
return people[0].name;
```

And you can loop over the items in the array like this:

```javascript
// Loop over the people array and return the names
// This will return ["Stevan", "Kelsey"]
var names = people.map(function(person){
  return person.name;
});

return JSON.stringify(names);
```

Again, there is a lot more to learn about arrays, but I hope this gives you a basic understanding of what they are and how they work. If you're interested in learning more, I recommend checking out the [MDN Web Docs](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Indexed_collections). This should though give you enough of an understanding to follow how they are used in the context of `RefToArray`.

## Putting It All Together

Now that we have a basic understanding of reference fields, objects, and arrays, we can start to make sense of `RefToArray`. At its core, `RefToArray` is a function that allows you to transform and aggregate data from related records. It takes two arguments: the first argument is the reference field that links the records together, and the second argument is an object that defines the fields you want to include in the resulting array. Here's an example of how you might use `RefToArray` to get a list of tasks that are linked to a project:

```javascript
var tasks = RefToArray({:Tasks}, { status: {:item::Status}, dueDate: {:item::Due Date} });

// returns an array of objects like this:
// [
//   { status: "In Progress", dueDate: "2024-11-08" },
//   { status: "Not Started", dueDate: "2024-11-10" },
// ]
return JSON.stringify(tasks);
```

> [!NOTE]
> The second argument to `RefToArray` is often the most confusing part for folks. I've found the easiest way to conceptualize it is as a template for the objects you want to create from each of the related records in the reference field. The keys in the object are the names of the fields you want to have in the resulting objects and the values are fields from the related records whose values you want to include in the resulting objects.

This makes `RefToArray` primarily a utility function that allows you to cross that boundary between records and data in Onspring and data structures in JavaScript that allows you to manipulate that data in a way you couldn't otherwise.

And remember now that your record data is in an array of objects you can use them in all the ways you would expect to be able to use an array of objects. You can filter them, sort them, map them, reduce them, etc. Which is what is being done in the example at the beginning of this post.

## Cursed with Knowledge

Okay, so now you know what `RefToArray` is and how it works. This is great, but it also can lead you down a dark and dangerous path. You see, now that you know how to use `RefToArray` you might be tempted to use it everywhere. Kind of one of those "when all you have is a hammer, everything looks like a nail" situations. But I'm here to tell you that you should resist that temptation. `RefToArray` is a powerful tool, but it is not always the right tool for the job. In a lot of cases you can get by with simpler formulas that don't require the complexity of `RefToArray`. So let's talk about when you would and wouldn't want to use it.

### Wait, Don't Do That

The most common mistake with `RefToArray` is using it when you don't need to. For example:

- If you only need to access a single fields value from related records
- If you only need to get the count, average, max, min, or sum of a field from related records
- If you only need to conditionally count or sum a field from related records

All of these can be done with simpler formulas or built-in functions. For example, if you only need to get the count of related records you can use the `Count` function. If you only need to get the sum of a field from related records you can use the `Sum` function. If you only need to conditionally count or sum a field from related records you can use the `CountIf` or `SumIf` functions. These functions are simpler and more efficient than using `RefToArray` in these cases.

```javascript
CountIf({:Tasks::Status}!=[:Complete],{:item::Record Id})
```

> [!NOTE]
> A lot of time I see folks using `RefToArray` to do conditional aggregations when they need to consider more than one field in the related records. You don't need to though. The trick is to move your conditional logic into a formula field in the related record itself and then use the `SumIf` or `CountIf` functions to aggregate the results. This is a much more efficient way to do it. I've wrote about this approach in more detail [here](/countifs-sumifs-in-onspring).

The other big red-flag usage of `RefToArray` is when you are working with a large number of related records. `RefToArray` can be slow when working with a large number of records because it has to fetch all of the related records, load them into memory, and transform them into an array of objects so that you can work with them in your formula. Then the formula engine has to actually run your formula to perform the operations you want to do which may or may not involve things that are computationally expensive. Therefore I think it is best to avoid using `RefToArray` in these cases and if you absolutely have to you should give some thought to how you can optimize your formula to make it efficient - which is a whole other post in itself.

## A Perfect Fit

You are excited to use `RefToArray` now, right? Especially after I've provided such a wonderful cautionary tale. 😅

Jokes aside though there are most definitely times at which you can't avoid it or it is actually the best tool for the job. I doubt I'll be able to cover all the concrete use cases where `RefToArray` is the right choice, but I can give you a pattern you can look for that might indicate that you've wondered into `RefToArray` territory. This is a pattern that I've seen in my own work and in the work of others that I think is a good indicator that you probably will need to use `RefToArray`.

### Siblings Need To Know About Each Other

Most commonly I've found that `RefToArray` comes up when the answer to the question you need your formula field to answer requires that each of your related records know about each other. This is a bit of a weird concept to wrap your head around at first. Intuitively you might think "what do you mean they need to know about each other? They're related to the same record, of course they know about each other." But that isn't the case. Each of the related records isn't aware of the other unless they are evaluated in the context of their parent record in the same formula field.

For example, let's say you have a Tasks app and a Projects app. Each task is linked to a project and from the project perspective you want to know the next upcoming due date of all the tasks. This requires that the tasks know about each other so that we can compare their due dates, filter out any task that are not after the current date, and then sort them to find the next upcoming due date. Something like this:

```javascript
// create an array of tasks
// with their date values.
// i.e. 
// [
//   { date: 6/23/2023 },
//   { date: 8/3/2023 },
//   { date: 12/15/2023 },
//   { date: 1/8/2024 },
//   { date: 3/23/2024 }
// ]
var tasks = RefToArray(
  {:Tasks}, 
  { date: {:item::Due Date} }
);

// filter out any tasks whose 
// dates are before or on today
// i.e. will return:
// [
//   { date: 1/8/2024 },
//   { date: 3/23/2024 }
// ]
var filtered = tasks.filter(function (p) {
  return IsAfterToday(new Date(p.date));
});

// if there are no tasks
// for us to consider return null.
if (filtered.length == 0) {
  return null;
}

// sort dates in ascending order
// according to their value in milliseconds
// i.e. will return:
// [
//   { date: 1/8/2024 },
//   { date: 3/23/2024 }
// ]
var sorted = filtered
  .slice()
  .sort(function (a,b) {
    return new Date(a.date).getTime() - new Date(b.date).getTime();
  });

// return date of first task in sorted
// array
// i.e. if today's date is 12/22/2023 it will return
// 1/8/2023
return Object(sorted[0]).date;
```

Again that is definitely a "for instance" example and not an exhaustive list of all the times you might need to use `RefToArray`. But it illustrates the point that if you find yourself in a situation where you are needing to perform some sort of aggregation or manipulation that requires related record A to consider and know about related record B then you are probably going to have to become familiar with `RefToArray`.

## Conclusion

I hope this post has helped you understand what `RefToArray` is and how it works. It can be a powerful tool in your formula toolbox, but it is not always the right tool for the job. You should use it judiciously and consider simpler alternatives when possible. When you do need to use `RefToArray`, make sure you have a good understanding of reference fields, objects, and arrays so that you can work with the data effectively. And remember, if you are dealing with large numbers of related records you should prioritize optimizing your formula and remain aware of the performance challenges you may face.
