```json meta
{
  "title": "Making Visual Regression Testing More Manageable with Playwright",
  "lead": "Dealing with dynamic content in visual regression testing can be tricky, but Playwright's feature set makes it so much easier. Learn how I used response interception and modification to ensure accurate screenshot comparisons and more reliable test results.",
  "isPublished": true,
  "publishedAt": "2024-11-30",
  "openGraphImage": "posts/making-visual-regression-testing-more-manageable-with-playwright/og-image.png",
}
```

I've been working to add a set of automated end-to-end smoke tests to cover reports within [Onspring](https://www.onspring.com/). Reports are a key feature of the platform and one of the key aspects of the reports is the ability to present the data within the reports in visually appealing and interesting ways using a variety of chart types. Initially I had taken the approach of building the reports and then asserting that particular aspects of the report were present in the DOM with the expected data. It worked, but then I remembered that [Playwright](https://playwright.dev/) has a whole set of features for doing visual regression testing and I hadn't yet had a good opportunity to put them to use. This use case seemed like a perfect fit.

> [!NOTE]
> This post is going to talk about the Node.js API for Playwright. However, Playwright also has APIs for other languages like Python and C#. The concepts should be similar across all of the APIs, but the specifics will be different.

## The Plan

Playwright provides a couple different tools that you can use for visual regression testing. Ways that you can capture the state of a page or part of a page and compare it to a previous state of that same thing. They primarily offer two ways to accomplish this. The first involves taking what they refer to as a snapshot which allows for comparing both text or any arbitrary binary data. This makes snapshots a bit more flexible and can be used for more than just visual regression testing. The second way is to take a screenshot which specifically involves comparing screenshots of the page or part of the page. I opted to use screenshots for my use case as our users are primarily concerned with the visual appearance of the reports and ensuring that they continue to display the same across releases given the same underlying data is important.

Playwright makes it pretty easy to accomplish this. You essentially write your test as you normally would and then include an assertion on a particular locator that you want to use for the screenshot. You can read more about the particular assertion [here](https://playwright.dev/docs/api/class-pageassertions#page-assertions-to-have-screenshot-1) along with details about the specifics of performing visual comparisons with Playwright [here](https://playwright.dev/docs/test-snapshots). Once you use this assertion the first time you run your test Playwright will take a screenshot and store it along side the test. On subsequent runs it will use that screenshot to compare against the current state of the page and if there are any differences it will fail the test.

Here is an example of what this looks like in one of my tests:

```typescript
test(
  'Configure a bar chart',
  {
    tag: [Tags.Snapshot],
  },
  async ({ appAdminPage, sourceApp, addContentPage, editContentPage, reportAppPage, reportPage }) => {
    test.info().annotations.push({
      description: AnnotationType.TestId,
      type: 'Test-608',
    });

    const fields = getFieldsForApp();
    let records = buildRecords(fields.groupField, fields.seriesField);

    await test.step('Setup source app with fields and records', async () => {
      await addFieldsToApp(appAdminPage, sourceApp, Object.values(fields));
      records = await addRecordsToApp(addContentPage, editContentPage, sourceApp, records);
    });

    const report = new SavedReportAsChart({
      appName: sourceApp.name,
      name: FakeDataFactory.createFakeReportName(),
      chart: new BarChart({
        visibility: 'Display Chart Only',
        groupData: fields.groupField.name,
      }),
    });

    await test.step("Navigate to the app's reports home page", async () => {
      await reportAppPage.goto(sourceApp.id);
    });

    await test.step('Create the report', async () => {
      await reportAppPage.createReport(report);
      await reportAppPage.page.waitForURL(reportPage.pathRegex);
      await reportPage.waitUntilLoaded();
    });

    await test.step('Verify the bar chart displays as expected', async () => {
      await expect(reportPage.reportContents).toHaveScreenshot();
    });
  }
);
```

Most of this test is setting up the data that I need to create the report and then creating the report. The important part is the last step where I use the `toHaveScreenshot` assertion. This is what tells Playwright to take a screenshot of the element that I'm asserting on. In this case it's the portion of the page that contains the chart that I'm interested in. Crazy straightforward right?

## The Problem

My plan was working great until I encountered a small hiccup with a couple of the chart types that we support. The chart types in question are complex charts that are actually built from two separate charts - either a column chart or stacked column chart with a line chart overlaid on top. Something like these:

![Column Chart](posts/making-visual-regression-testing-more-manageable-with-playwright/column-chart.png)

![Stacked Column Chart](posts/making-visual-regression-testing-more-manageable-with-playwright/stacked-column-chart.png)

The problem with these charts is that they use the name of the data source - we call them apps - for the line chart as its label in the legend. This means each test run when I create the line chart's underlying app I can't really use a different name each time like I normally would. The reason being that means that the current screenshot that Playwright captures during the test is never going to match the previous one.

The simple solution was just to use the same name for the line chart's app each time the test runs. This would ensure that the screenshot comparison would work as expected. However, this approach breaks down when I want to run the test in parallel. That is when I want to be able to run the test multiple times at the same time within the same instance. I found this out the hard way because as part of my CI process when adding a new test I require that the test runs 3 times successfully before I consider it stable.

What to do then? I found myself in this rock and a hard place. I've got these immovable constraints:

- the data source name has to remain stable across runs so that the screenshot comparison works
- the data source name has to be unique across parallel test runs as each test run is creating its own data source

Lucky for me Playwright has got it covered.

## The Solution

In my mind the ideal solution involved me being able to just modify that one piece of the chart that is rendered on the page without really affecting the rest of the test flow. I mean what good is an end-to-end smoke test if I start to do hacky things to make it work?

The answer for me was to use Playwright's [response interception](https://playwright.dev/docs/release-notes#response-interception) feature that combines the API testing capabilities of Playwright with it's request interception capabilities. This allows me to intercept the response from the server and modify it before it gets to the browser.

This is perfect for my use case because I can intercept the response that contains the data source name and modify it to be the same each time the test runs. This way the screenshot comparison will work as expected and I can run the test in parallel. However I still can test the full end-to-end flow of the test including asserting that the data source name I get back from the server is what I would expect.

Here is what a test looks like using this approach:

```typescript
test(
  'Configure a column plus line chart',
  {
    tag: [Tags.Snapshot],
  },
  async ({ appAdminPage, addContentPage, editContentPage, reportAppPage, reportPage, sysAdminPage }) => {
    test.info().annotations.push({
      description: AnnotationType.TestId,
      type: 'Test-617',
    });

    // The name of the source app needs to be unique to avoid conflicts...but
    // it also needs to be consistent across test runs so that snapshots can be compared.
    const projectName = test.info().project.name;
    const appName = FakeDataFactory.createFakeAppName();
    const mockAppName = `configure_a_column_plus_line_chart_${projectName}`;
    const sourceApp = await createApp(sysAdminPage, appName);
    appsToDelete.push(appName);

    const fields = getFieldsForApp();
    let records = buildRecords(fields.groupField, fields.seriesField);

    await test.step('Setup source app with fields and records', async () => {
      await addFieldsToApp(appAdminPage, sourceApp, Object.values(fields));
      records = await addRecordsToApp(addContentPage, editContentPage, sourceApp, records);
    });

    const lineReport = new SavedReportAsChart({
      appName: sourceApp.name,
      name: FakeDataFactory.createFakeReportName(),
      chart: new LineChart({
        visibility: 'Display Chart Only',
        groupData: fields.groupField.name,
      }),
    });

    const columnReport = new SavedReportAsChart({
      appName: sourceApp.name,
      name: FakeDataFactory.createFakeReportName(),
      chart: new ColumnPlusLineChart({
        visibility: 'Display Chart Only',
        groupData: fields.groupField.name,
        seriesData: fields.seriesField.name,
        lineChart: lineReport,
      }),
    });

    await test.step("Navigate to the app's reports home page", async () => {
      await reportAppPage.goto(sourceApp.id);
    });

    await test.step('Create the line report', async () => {
      await reportAppPage.createReport(lineReport);
      await reportAppPage.page.waitForURL(reportPage.pathRegex);
    });

    await test.step('Navigate back to the app reports home page', async () => {
      await reportAppPage.goto(sourceApp.id);
    });

    await test.step('Create the column report', async () => {
      await reportPage.page.route(
        /Report\/\d+\/GetReportDisplayConfig/,
        async route => await mockLineChartSeriesName(route, appName, mockAppName),
        { times: 1 }
      );

      await reportAppPage.createReport(columnReport);
      await reportAppPage.page.waitForURL(reportPage.pathRegex);
      await reportPage.waitUntilLoaded();
    });

    await test.step('Verify the column plus line chart displays as expected', async () => {
      await expect(reportPage.reportContents).toHaveScreenshot();
    });
  }
);
```

Again much of this test is setting up the data that I need to create the report and then creating the report. The important part is the route that I set up to intercept the response from the server and modify it. In my case the response is a JSON object and since I need to do this same thing for another test I just created a helper function to do the actual modification. Here is what that function looks like:

```typescript
async function mockLineChartSeriesName(route: Route, appName: string, mockAppName: string) {
  const response = await route.fetch();
  const body = await response.json();
  const lineChartSeries = body.chartConfig.chartConfigData.dataset.find(
    (d: { seriesName: string }) => d.seriesName === appName
  );

  if (lineChartSeries === undefined) {
    throw new Error(`Series with name ${appName} not found in the chart config.`);
  }

  lineChartSeries.seriesName = mockAppName;

  await route.fulfill({ response, json: body });
}
```

How lovely is that? I can now ensure consistency across test runs when comparing screenshots, maintain the ability to run the test in parallel, and still have confidence that the data source name is what I expect it to be despite modifying it. Just a really great example of how Playwright as a framework makes it so much easier to do visual regression testing and make it manageable.

## Conclusion

I feel like I'm becoming quite the Playwright fanboy, but I can't help it. It's just such a great tool for doing end-to-end testing. The API is so easy to use and the features that they provide are just so powerful. I'm really glad that I was able to find a solution to my problem that didn't involve me having to compromise on the quality of the test or come up with a bespoke, hacky, and difficult to maintain solution. And I hope that this post has been helpful to you if you find yourself in a similar situation.
