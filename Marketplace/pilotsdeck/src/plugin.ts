import streamDeck, { LogLevel } from "@elgato/streamdeck";

import { Installer } from "./actions/installer";

// We can enable "trace" logging so that all messages between the Stream Deck, and the plugin are recorded. When storing sensitive information
streamDeck.logger.setLevel(LogLevel.TRACE);

streamDeck.actions.registerAction(new Installer());

// Finally, connect to the Stream Deck.
streamDeck.connect();
