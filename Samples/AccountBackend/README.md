# AccountBackend
This project demonstrates using Servya to build a full user account system.

# Conventions
## Responses
All service methods return either `Response` or `Response<T>`. Both the `Status` enum and the `T` type parameter are implicitly convertible to responses.

All responses are serialised to JSON.

```cs
enum Status
{
	Ok,
	WeHateBob
}

[Service]
class GreeterService
{
	[UnprotectedRoute]
	public Response<string> SayHi(string name)
	{
		// We really don't like this guy
		if (name == "Bob")
			return Status.WeHateBob;
		else
			return "Hey there, " + name;
	}
}
```

http://host/greeter/sayhi?name=ruan: `{ "Code": 0, "Info": "Ok", "Payload": "Hey there, ruan" }`

http://host/greeter/sayhi?name=bob: `{ "Code": 1, "Info": "WeHateBob" }`

## Tokens
When a user logs in to the account backend, they'll receive a session token which is valid for 10 minutes. You can create a new service method which requires the user to be logged in by decorating it with `[TokenRoute]`.

```cs
[Service]
class ProfileService
{
	// missing service ctor, etc

	[TokenRoute]
	public async Task<ProfileInfo> My(string user)
	{
		var hashes = await m_db.HashGetAllAsync(Keys.User(user));
		var data = hashes.ToStringDictionary();
	
		return new ProfileInfo { Name = user, JoinDate = long.Parse(data["joindate"]) };
	}
}
```

The above `/profile/my` method would actually be called like `http://host/profile/my?token=blah`; the `TokenValidator` class handles token validation and filling in the `string user` parameter before your service method is even called.
