# IEProxyTest
A test project demonstrating a regression in Internet Explorer's proxy handling

To use this project:

1. Create a locally hosted website. A very easy way to accomplish this is to build
   the Selenium project's test web server
   * Clone the Selenium repo
   * Build the server with the target `buck build //java/client/test/org/openqa/selenium/environment:webserver`
   * Run it with `java -cp buck-out\gen\java\client\test\org\openqa\selenium\environment\webserver.jar org.openqa.selenium.environment.webserver.JettyAppServer`
   This will give a web server running at `http://localhost:2310/common`
2. Create hosts entries in your hosts file at `C:\Windows\System32\driver\etc\hosts` for
   `www.seleniumhq-test.test` and `www.seleniumhq-test-alternate.test` that point back to `127.0.0.1`
3. Open the included solution in Visual Studio
4. Run the solution

The test will create a local proxy server, launch Internet Explorer, update the system settings
to point to the proxy server, navigate to three URLs (one that should be bypassed, one that is 
hosted locally that should be proxied, and a remote URL that should have traffic proxied), shut
down everything, and print out a list of all of the resources proxied.

We should see resources not proxied through the proxy, and two sites proxied through the proxy.
In current version of IE, you'll see the all localhost resources are not proxied, regardless of
the type of URL used to access them.
