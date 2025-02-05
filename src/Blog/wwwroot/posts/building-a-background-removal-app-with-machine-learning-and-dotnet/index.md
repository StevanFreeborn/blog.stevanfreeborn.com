```json meta
{
  "title": "Building a Background Removal App with Machine Learning and .NET",
  "lead": "Learn how to create a powerful background removal app using machine learning models and a .NET console application. This step-by-step guide covers everything from model integration to deployment, perfect for developers looking to enhance their skills in AI and .NET.", "isPublished": true,
  "publishedAt": "2025-02-05",
  "openGraphImage": "posts/building-a-background-removal-app-with-machine-learning-and-dotnet/og-image.png",
}
```

I've been using [Canva](https://www.canva.com/) for a few years now when I want to create images for content I'm sharing or posting online. The UI is great and they offer a lot of templates for all the different sizes that platforms prefer. It'd been a bit though since I'd used it heavily so when I picked it back up this past month to create thumbnails for the videos I've been posting on my [YouTube channel](https://www.youtube.com/@stevanfreeborn) they offered me a free trial of their premium service. I've been using it for the last couple weeks and really the main feature included in their premium service I've been using is their background removal. I know how to do background removal in Photoshop and GIMP, but Canva's makes it so easy and fast with just a click of a button. However with the trial expiring I just can't justify another subscription for a single feature. That is when the engineer in me kicked in and I thought, "Surely I can figure out how to do this myself." So here we are. 😅

## Where to Start

I've done a small bit of image manipulation like this previously when taking [this course](https://www.dukelearntoprogram.com/course1/index.php). We basically implemented the ability to replace the background of an image taken in front of a green screen with a different image. So conceptually I have a grasp on how to work with an image and its pixels. However, for much of my use cases I am not needing to replace a background, but actually determine what is the background in an image and remove it. This way I can layer the subject of the image on top of a different background. So what do you do when you don't know how to do something? You get to learning. I started with a Google search and quickly realized this is a broad and deep topic that has entire fields of study dedicated to it. In general though, there are two main approaches to the problem: 

- traditional techniques that rely on handcrafted heuristics around pixel colorrs and shapes to determine the background of an image
- machine learning techniques that rely on training a model on a large image dataset and optimizing it to perform a particular task on the images such as background removal.

