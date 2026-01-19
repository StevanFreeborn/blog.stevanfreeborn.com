```json meta
{
  "title": "Yours, Mine, Ours: A Guide to Rust Ownership",
  "lead": "Rust promises memory safety without a garbage collector, but the price of admission is understanding Ownership. This post breaks down the rules of ownership, borrowing, and the borrow checker using a simple mental model.",
  "isPublished": true,
  "publishedAt": "2026-01-19",
  "openGraphImage": "posts/yours-mine-ours-rust-ownership/og-image.png"
}
```

If you are coming from C++ or C, you are probably used to managing memory manually. I've not written either of those languages, but I've heard this can often lead to shooting yourself in the foot with a segfault. If you are coming from Java or C#, which is much more familiar to me, then you are used to relying on a garbage collector to clean up after you at the cost of random performance pauses.

Rust offers a third way. It promises memory safety without a garbage collector. But the price of admission is understanding its most unique feature: **Ownership**.

To understand Ownership, I find it helpful not to think about computer memory, but instead to think about something like a physical book. If I hand you a book, I don't have it anymore. I can't read it, I can't write in it. You have it.

Rust largely treats data in a similar way to that book.

## The Three Rules

There are only three rules you really need to memorize:

1. Each value in Rust has a variable that's called its owner.
1. There can only be one owner at a time. No shared custody.
1. When the owner goes out of scope the value is dropped and memory is freed immediately.

## Move Semantics

Let's look at this in code. This is what we call "move semantics."

```rust
let owner_one = String::from("The Rust Book");
let owner_two = owner_one;
```

In most languages both variables would now point to the same data. But in Rust, `owner_one` has **transferred** ownership to `owner_two`.

Because of rule number two above, `owner_one` is now invalid. If you tried to access it the compiler would stop you because it's gone.

## Immutable Borrowing

Obviously though in real programming we can't just move data around forever. Sometimes we just want to look at it so we can make a decision or perform some operation with it. This is what Rust calls **Borrowing**.

```rust
let owner = String::from("The Rust Book");
let borrower = &owner;

println!("I borrowed: {} from the owner!", borrower);
println!("The owner still has: {}", owner);
```

Notice the ampersand (`&`). We aren't taking the book, but rather we are just "borrowing" it so we can read it. Because we didn't transfer ownership, both variables remain valid. The owner still owns it, and the borrower can read it.

## Mutable Borrowing

What if we want to change the data without taking ownership? For example, I just want to borrow the book and add my notes in it. In this case we need a **Mutable Borrow**.

```rust
let mut owner = String::from("The Rust Book");
let borrower = &mut owner; // I'm handing you the book AND a pen

borrower.push_str(" - Second Edition");
```

We define `owner` as `mut` - indicating it can be mutated - and create a borrower that is a mutable reference (`&mut`). This is like me handing you the book with a pen and asking you to leave any notes you have in the margins. You don't own the book, but I'm giving you explicit permission to change it while you have it.

## The Tricky Bit

Rust is very strict when it comes to borrowing. This is how it helps enforce ownership and protect us from a whole class of bugs when we start dealing with concurrency.

- You can have as many **immutable** borrowers as you want or you can have exactly **one mutable** borrower.

In other words as many people can read the book at the same time, but only one can write in it at a time and no one can write in it while others are reading it. For example, this would not be allowed.

```rust
let mut owner = String::from("The Rust Book");

let reader = &owner;
let writer = &mut owner; // !! Compile Error !!
```

I think this makes sense I think intuitively. It is a good thing to not be able to modify the book while someone is reading it. This is how Rust prevents race conditions at compile time. It is a bit annoying at first when you run into these limitations, but the trade-off is well worth it in my opinion.

## Do What When?

The natural question you have once you grok ownership from a conceptual level is how to decide when to borrow or not and when something should be mutable or not. Here is how I've thought about it as I've been learning Rust.

- Do I need to delete or consume the data? Cool, I'll take ownership then. (`T`)
- Do I need to edit it and do others need to know about that? Well let me borrow then and mutate. (`&mut T`)
- Do I just need to read it? Borrowing it all the way. (`&T`)
- Are we talking about primitives that I need to read? Let Rust just copy it and I'll use it.

## Concrete Examples

### Immutable Borrow

The `calculate_length` function just needs to measure the string. It doesn't need to own it. So we pass an immutable slice. The string stays with `my_string`.

```rust
fn calculate_length(s: &str) -> usize {
    s.len()
}

let my_string = String::from("Hello, Rust!");
let length = calculate_length(&my_string);

println!("The length of '{}' is {}.", my_string, length);
```

### Mutable Borrow

The `add_signature` function needs to modify the document, but it doesn't need to own it. So we pass a mutable reference. The original document is updated.

```rust
fn add_signature(doc: &mut String) {
    doc.push_str("\nSigned: Stevan");
}

let mut my_document = String::from("This is an important document.");

add_signature(&mut my_document);

println!("{}", my_document);
```

### Transfer Ownership

Here, we are creating a `Book` struct. The `create_book` function takes ownership of the title and author strings and returns a new `Book`. The caller transfers ownership of the strings to the function which then uses them to create the `Book` and returns ownership of the `Book` back to the caller.

```rust
struct Book {
    title: String,
    author: String,
}

fn create_book(title: String, author: String) -> Book {
    Book { title, author }
}

let my_title = String::from("The Rust Programming Language");
let my_author = String::from("Steve Klabnik and Carol Nichols");
let my_book = create_book(my_title, my_author);

println!("Created book: '{}' by {}", my_book.title, my_book.author);
```

### Copy

For simple types like integers, Rust uses the `Copy` trait. This means that when you pass an integer to a function, it gets copied rather than moved. The original variable remains valid after the function call.

```rust
fn double_number(x: i32) -> i32 {
    x * 2
}

let my_number = 10;
let doubled = double_number(my_number);

println!("Original number: {}, Doubled: {}", my_number, doubled);
```

## Conclusion

It feels restrictive at first. The borrow checker *will* yell at you. You *will* get frustrated. You'll feel like you are playing a game of whack-a-mole. That means you are likely still learning and just trying to hard to fight the borrow checker. Stiack with it.

**Ownership is the price we pay for memory safety without a garbage collector**. With enough reps you'll begin to internalize the rules, you'll stop fighting the compiler, and you'll have code that is fast **AND** safe by default because of it.
