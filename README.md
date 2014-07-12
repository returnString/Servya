# Servya
Servya is a framework designed for developing servers using .NET and Mono. Its main focus is building web APIs.

Master: [![Build Status](https://travis-ci.org/returnString/Servya.svg?branch=master)](https://travis-ci.org/returnString/Servya)

# Features

* Async all the way down
* Supports both event-driven (Node.js) and thread-per-request (Apache) modes
* No callback hell!
* Built-in dependency injection (no config required)
* Simple service model: just use classes and methods to model your API

# Hello World
```cs
[Service]
class ProgramService
{
	// Available as: http://host/program/hello?name=whatever
	[Route]
	public string Hello(string name)
	{
		return "Hello there, " + name;
	}

	// Available as: http://host/program/belatedhello?name=whatever&delay=2
	// The response will take <delay> seconds to come through, but this won't block server resources
	[Route]
	public async Task<string> BelatedHello(string name, int delay)
	{
		await Task.Delay(TimeSpan.FromSeconds(delay));
		return "Apologies for keeping you waiting, " + name;
	}

	static void Main()
	{
		App.Run();
	}
}
```
