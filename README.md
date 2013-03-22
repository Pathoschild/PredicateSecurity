**PredicateSecurity** provides relational security on content collections (including deferred LINQ
queries for NHibernate or Entity Framework). This lets you define security groups and permissions
using LINQ criteria, and then filter collections based on these security rules.

You can also define global (non-relational) security permissions, such as for site administrators.
These permissions are inherent to the user object.

## Example
### Relational security
For example, let's say you're building a blog app and want to define some complex access rules.
Readers can see any published post, and editors can see their own draft posts and any draft post
they're invited to contribute to. Blog editors can even block specific users, so they can't see
their blog post. To make matters worse, these rules are all completely configurable and you have
plenty of other resources (not only blog posts).

This is a pretty complex scenario to support, but PredicateSecurity makes it pretty easy. First you
configure your security rules:
```c#
var security = new PredicateFilter<int>();

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
IQueryable<BlogPost> canRead = security.Filter(blogs, "read", user.ID);
IQueryable<BlogPost> canEdit = security.Filter(blogs, "edit", user.ID);
```

### Global security
You can define non-relational security permissions (such as for a _site administrator_) using the
`PredicateFilter<TUser, TUserKey>`. The previous example uses the `Predicate<TUserKey>`,
which is just a simplified wrapper around this class. This filter needs two more configurations:
how to get the user key (used in predicates), and how to get the global permissions for a user.

For example, let's say you want to add a site administrator to the above example. You have a `User`
object which has a simple list of global permissions as `User.Permissions`. You first configure
the filter with your `User` object:
```c#
var security = new PredicateFilter<User, int>(
   user => user.Key,
   user => user.Permissions.ToDictionary(permission => permission, permission => PermissionValue.Allow)
);
```

And now you can use it the same way, passing in the `User` object directly:
```c#
// what blog posts can I read or edit (given the security groups I'm a member of)?
IQueryable<BlogPost> canRead = security.Filter(blogs, "read", user);
IQueryable<BlogPost> canEdit = security.Filter(blogs, "edit", user);
```
