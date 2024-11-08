```json meta
{
  "title": "Onspring's RefToArray Formula Function Explained: The What, Why, and How",
  "lead": "Learn how to use the RefToArray function in the Onspring formula engine to transform and aggregate data from related records. This guide breaks down how RefToArray works, offers practical use cases, and explains when to leverage it for custom data evaluation. Whether you're handling risk scores, survey responses, or task deadlines, get tips on harnessing RefToArray for data insight and control without giving yourself a headache",
  "isPublished": true,
  "publishedAt": "2024-11-08",
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
