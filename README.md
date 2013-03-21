**PredicateSecurity** provides relational security on content collections (including deferred LINQ
queries for NHibernate or Entity Framework). This lets you define security groups and permissions using LINQ criteria, and then filter collections based on these security rules.

## Example
For example, let's say you're building a blog app and want to define some complex access rules.
Readers can see any published post, and editors can see their own draft posts and any draft post
they're invited to contribute to. Blog editors can even block specific users, so they can't see
their blog post. To make matters worse, these rules are all completely configurable and you have
plenty of other resources (not only blog posts).

This is a pretty complex scenario to support, but PredicateSecurity makes it pretty easy. First you
configure your security rules:
```c#
PredicateSecurity<int> security = new PredicateSecurity<int>();

// define some security group rules (who is part of each group?)
security
   .AddGroup<BlogPost>("reader",      (blog, userId) => !blog.Draft)
   .AddGroup<BlogPost>("submitter",   (blog, userId) => blog.Submitter.ID == userId)
   .AddGroup<BlogPost>("contributor", (blog, userId) => blog.Contributors.Any(p => p.ID == userId))
   .AddGroup<BlogPost>("banned",      (blog, userId) => blog.BannedUsers.Any(p => p.ID == userId));

// define some permission rules (who can do what?)
security
   .AddPermission("reader",      "read", PermissionValue.Allow)
   .AddPermission("submitter",   "read", PermissionValue.Allow)
   .AddPermission("submitter",   "edit", PermissionValue.Allow)
   .AddPermission("contributor", "read", PermissionValue.Allow)
   .AddPermission("contributor", "edit", PermissionValue.Allow)
   .AddPermission("banned",      "read", PermissionValue.Deny);
```

Now you can filter any collection of blog posts using your security rules. You can even do this on
a deferred LINQ query from NHibernate or Entity Framework, so your rules will be converted into
optimized SQL.
```c#
// get all blogs in the application (deferred NHibernate query)
IQueryable<BlogPost> blogs = session.Query<BlogPost>();

// what blog posts can I read or edit (given the security groups I'm a member of)?
var canRead = security.Filter(blogs, "read", user.ID);
var canEdit = security.Filter(blogs, "edit", user.ID);
```