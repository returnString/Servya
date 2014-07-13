# AccountBackend
This project demonstrates using Servya to build a full user account system.

# Conventions
All service methods return either `Response` or `Response<T>`. Both the `Status` enum and the `T` type parameter are implicitly convertible to responses.

All responses are serialised to JSON.

```cs
enum Status
{
	Ok,
	WeHateBob
}

class GreeterService
{
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
