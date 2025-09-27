Interactions, InteractAgents, and Interactables

To interact is when one GameObject is near another, and some user input is entered (by default, hitting the space bar), triggering some effect. 
To interact, three scripts must be properly set up: An InteractAgent, one or more Interactables, and some Interaction. 

An InteractAgent is a script that allows one GameObject to interact with Interactables. Every frame, the InteractAgent checks whether any of its known Interactables are within the interact distance. If multiple are, the InteractAgent selects the closest. Then, if the player is performing some action, the InteractAgent will interact with the Interactable. 

An Interactable is a script that allows a GameObject to be interacted with. For an Interactable to function properly, it must be registered with an InteractAgent. By default, an InteractAgent will automatically register itself with the default InteractAgent. A specific InteractAgent can be also specified from the editor. Any InteractAgents that have this Interactable registered will be able to call Interact() on this Interactable, causing some effect. The effect caused is specified based on what Interaction is passed to this Interactable via the Editor. An Interactable is also able to detect whether or not it is the closest possible Interactable for some InteractAgent, although it does not specify any action to take in this case. 
Interactable has a notable child class, DisplayedInteractable. A DisplayedInteractable shows a GameObject specified in the editor when an InteractAgent is nearby, and hides it otherwise. This may be useful to provide some display when the InteractAgent is within interact range. 

Interaction is an abstract class, representing some action that an Interactable can take in response to an InteractAgent. Its only abstract method that must be implemented is 
| abstract private protected void StuffToDo();
which decides what action will be taken when an Interactable containing this Interaction is interacted with. When an Interaction is complete, it may trigger some other Interaction specified in the editor, essentially allowing for chained or compound Interactions. 
Some Interaction implementation classes: 
- DialogueInteraction: Sends some text to a Dialogue object. 
- LogInteraction: Sends some text to Debug.Log
- NewSceneInteraction: Switches to some new scene. 
- To Be Implemented: 
  - AnimationInteraction: Plays some animation. 
  - CardInteraction: Changes the cards the player has. 

A common example use case would be a player interacting with NPC's, causing some dialogue to be displayed on the screen. The player prefab would have an InteractAgent. Several NPC prefabs would be present, each with an Interactable linked to that InteractAgent. Each Interactable would also be linked to an instance of a DialogueInteraction with the appropriate message. 
At the start of the scene, the NPCs' Interactables would link to the the InteractAgent, allowing the player to detect the NPC's. When the player got close to a particular NPC prefab and pressed the spacebar, the InteractAgent would detect it and signal the Interactable. The Interactable would then cause its DialogueInteraction to occur, displaying its text via the Dialogue system. 
