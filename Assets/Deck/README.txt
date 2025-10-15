Cards, Decks and Selecting

A Card is the basic unit of the game. A Card stores its face state (is it face-up, face-down, or entirely unknown?). A Card has abstract method Effect(), which is the effect that the card performs. A class that inherits from Card that shuffles a particular deck, for example, would probably have some code for shuffling the deck within its Effect() method. 

A Deck is a wrapper for a list of cards, with some extra methods. Cards may be transferred between decks, for example. One important thing to note is that a Card should always exist within a Deck. This is to make life simpler. A Deck also know what DeckPreset should be applied to it and is responsible for calling it whenever the contents of the deck changes. 

A DeckPreset represents the visual effects applied to a specific Deck of Cards. When the abstract method Apply(), the DeckPreset should set the position and face state of each card. 
Notable subclasses include:
- HiddenDeck, which represents a deck where all cards are completely invisible and in the same location. Only the top card is non-invisible. It may be face-up (the player can see what card it is) or face-down (the player only knows the card exists and cannot tell what card it is), depending on the bool showTop. 
- VisibleDeck, which represents a spread of cards. Cards are arranged in a line and evenly spaced (based on the Vector3 delta). All cards are face-up. 

A Selectable represents an option that a player can select. This can be something like selecting a particular card, or selecting the option to end a turn. 
A UserInput detects when a player selects a certain selectable, and returns these selections. 
- For more detailed description, see the documentation at the beginning of the UserInput class. 
- Card is a subclass of Selectable. This allows a user to select a Card, if prompted. 

=============
TODO!
A CardGame is a class that contains a Dictionary of all Decks valid in a given game. There should be one and only one active CardGame in a scene at any given time. This is so that if a Card's Effect()  would, for example, shuffle the draw pile, the CardGame can immediately tell whether there is a Deck that corresponds to the term "draw pile" in the game and, if necessary, forbid that card from being played. 
- If a particular Card's actions are impossible due to a Deck not existing, then a warning should appear above the Card. If the Card's Effect() method is actually called, the CardGame should detect this and end the game with the player losing. As such, any attempts to get or modify the Decks should be done through the CardGame, and not directly by the Cards. 
The CardGame class is also responsible for defining the rules that affect the flow of the player's turn. 
