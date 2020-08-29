2.1. In theory, there is a possibility to implement a Factory pattern in the
     case of asteroids/lasers creation. The GameLogic class would notify some
     kind of another a class to create a specific class instance/ class with
     certain proprties values.

2.2. The GameLogic class is somehow Singleton but not clearly as it can be
     created many times, however, I don't suppose that it will work with
     Unity-specific C#. Making more DataLayer-like interfaces won't be
     necessary, in my opinion. There should be a single and common interface
     for a single and specific task, like config loading (if we have various
     config sources) or in-game objects.
