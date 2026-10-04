This is one kind of note-taking app.
Name: Memorbit
The key modules are:
1. Library
2. Task
3. Finance
4. Goals

This project is developed on asp.net core web api. It follows clean architecture with vertical slicing architecture.
/Memorbit project is the main web project
/Memorbit.Presentation project has all the APIs that communicates with the outer world via HTTP
/Memorbit.Infrastructure project has infrastructure related implementation like Identity Auth, Database migration etc
/Memorbit.Application project has the core business logic
/Memorbit.Domain project has the base entities

In every project (except Memorbit main web project), the modules are separated by the folder/directory of the module names. like in application:
/Memorbit.Application
    - Library
    - Task
    - Finance
    - Goals

similarly, in other projects, module specific code lives in that module directory. 

