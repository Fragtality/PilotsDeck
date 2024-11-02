import streamDeck, { action, SendToPluginEvent, SingletonAction } from "@elgato/streamdeck";
import { spawn } from 'child_process';
import process from 'process';
import http from 'http';
import https from 'https';
import fs from 'fs';
import os from 'os';
import { resolve } from "path";
import { rejects } from "assert";


@action({ UUID: "com.extension.pilotsdeck.installer" })
export class Installer extends SingletonAction<Settings> {

	override async onSendToPlugin(ev: SendToPluginEvent<Payload, Settings>) {
		const destinationDir = os.tmpdir();
		process.chdir(destinationDir);
		const { file, fileUrl } = this.getReleaseInfo(await this.fetchReleaseInfo());
		// const file = 'Install-PilotsDeck-latest.exe';
		// const fileUrl = 'https://raw.githubusercontent.com/Fragtality/PilotsDeck/refs/heads/master/' + file;

		await this.download(fileUrl, file).then(() => {
			spawn(file, {shell: true});
		});
	}

	async fetchReleaseInfo() : Promise<string> {
		const request = await fetch("https://raw.githubusercontent.com/Fragtality/PilotsDeck/refs/heads/master/release-info");
		if (request?.status !== 200) {
			return '';
		}

		var result = await request.text();
		return result.replaceAll('\n','')
	}

	public getReleaseInfo(version: string) : {file : string, fileUrl: string} {
		var file = '';
		var fileUrl = '';

		if (version || version !== '') {
			file = 'Install-PilotsDeck-' + version + '.exe';
			fileUrl = 'https://github.com/Fragtality/PilotsDeck/releases/download/' + version + '/' + file;
		}

		return {file: file, fileUrl: fileUrl};
	}

	async download(url: string, filePath: string) {
		const proto = !url.charAt(4).localeCompare('s') ? https : http;

		var testResponse = await fetch(url);
		if (testResponse.redirected) {
			url = testResponse.url;
		}
	  
		return new Promise<void>((resolve, reject) => {
		  const file = fs.createWriteStream(filePath);
	  
		  const request = proto.get(url, response => {
			if (response.statusCode !== 200) {
			  fs.unlink(filePath, () => {
				reject(new Error(`Failed to get '${url}' (${response.statusCode})`));
			  });
			  return;
			}
	  
			response.pipe(file);
		  });
	  
		  file.on('finish', () => resolve());
	  
		  request.on('error', err => {
			fs.unlink(filePath, () => reject(err));
		  });
	  
		  file.on('error', err => {
			fs.unlink(filePath, () => reject(err));
		  });
	  
		  request.end();
		});
	  }
}

type Payload = {

};

type Settings = {

};
