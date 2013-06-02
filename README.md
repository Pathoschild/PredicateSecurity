**PredicateSecurity** provides relational security on content collections (including deferred LINQ
queries for NHibernate or Entity Framework). This lets you define security groups and permissions
using LINQ criteria, and then filter collections based on these security rules. The library also
supports global (non-relational) security permissions, such as for site administrators.

You can use the PredicateFilter in your project by referencing the
[`Pathoschild.PredicateSecurity` NuGet package](https://nuget.org/packages/Pathoschild.PredicateSecurity).

## Usage
### Relational security
The main goal of this library is to define _relational security permissions_. These are permissions
that depend on a user's relationship with the content. For example, the _blog submitter_ is the user
who submitted the blog post.

For example, let's say you're building a blog app and want to define some relational access rules.
Readers can see any published post, and editors can see their own draft posts and any draft post
they're invited to contribute to. Blog editors can even block specific users, so they can't see
their blog post. To make matters worse, these rules are all completely configurable and you have
plenty of other resources (not only blog posts).

This is a pretty complex scenario to support, but PredicateSecurity makes it pretty easy. First you
configure your security rules:
```c#
var security = new PredicateFilter<int>(); // int is the user identifier used in the predicates

// define some security group rules (who is part of each group?)
security
   .AddGroup<BlogPost>("blog-reader",      (blog, userId) => !blog.Draft)
   .AddGroup<BlogPost>("blog-submitter",   (blog, userId) => blog.Submitter.ID == userId)
   .AddGroup<BlogPost>("blog-contributor", (blog, userId) => blog.Contributors.Any(p => p.ID == userId))
   .AddGroup<BlogPost>("banned",           (blog, userId) => blog.BannedUsers.Any(p => p.ID == userId));

// define some permission rules (who can do what?)
security
   .AddPermission<BlogPost>("blog-reader",      "read-blog", PermissionValue.Allow)
   .AddPermission<BlogPost>("blog-submitter",   "read-blog", PermissionValue.Allow)
   .AddPermission<BlogPost>("blog-submitter",   "edit-blog", PermissionValue.Allow)
   .AddPermission<BlogPost>("blog-contributor", "read-blog", PermissionValue.Allow)
   .AddPermission<BlogPost>("blog-contributor", "edit-blog", PermissionValue.Allow)
   .AddPermission<BlogPost>("banned",           "read-blog", PermissionValue.Deny);
```

Now you can filter any collection of blog posts using your security rules. You can even do this on
a deferred LINQ query from NHibernate or Entity Framework, so your rules will be converted into
optimized SQL.
```c#
// get all blogs in the application (deferred NHibernate query)
IQueryable<BlogPost> blogs = session.Query<BlogPost>();

// what blog posts can I read or edit (given the security groups I'm a member of)?
IQueryable<BlogPost> canRead = security.Filter(blogs, "read-blog", user.ID);
IQueryable<BlogPost> canEdit = security.Filter(blogs, "edit-blog", user.ID);
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
IQueryable<BlogPost> canRead = security.Filter(blogs, "read-blog", user);
IQueryable<BlogPost> canEdit = security.Filter(blogs, "edit-blog", user);
```

### Testing a single content object
You can test a user's permission against a single content ibject. For example:
```c#
// can I read or edit _this_ blog post?
bool canRead = security.Test(blogPost, "read-blog", user);
bool canEdit = security.Test(blogPost, "edit-blog", user);
```

### Polymorphic content types
The predicate filter matches security against the exact content type; it doesn't support polymorphism
out of the box. That is, if you define a security group and permissions for `BlogPost` entities, you
can't check their permissions against `BlogPostSubclass` entities (unless you cast them).

If you need polymorphic support, you can enable group name reuse and configure the group for each type:
```c#
var security = new PredicateFilter<int>();
security.AllowReusingGroupNames = true;
security.AddGroup<BlogPost>("blog-reader", (blog, userId) => !blog.Draft);
security.AddGroup<BlogPostSubclass>("blog-reader", (blog, userId) => !blog.Draft);
```