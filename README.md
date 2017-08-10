# ASP.NET MVC Controller Action Security Checker

This is a Drop in ASP.NET MVC Controller and Action that displays any actions that modify resources (HTTP POST, PUT, DELETE, and PATCH) that do not have an Authorize or ValidateAniForgeryToken attributes applied.

## Usage

Add the `SystemController` file to your ASP.NET MVC project, make sure there's a route that'll reach it, and then visit it in a local instance of your site. It only shows up for localhost requests for security reasons.
