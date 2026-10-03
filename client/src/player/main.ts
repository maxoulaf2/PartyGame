import { mount } from 'svelte';
import { startErrorReporting } from '../shared/errors/errorReporting';
import '../shared/theme.css';
import './player.css';
import App from './App.svelte';

// First of all, so that an error of the first rendering is reported too.
startErrorReporting('Player');

const target = document.getElementById('app');
if (!target) {
    throw new Error('Missing #app element');
}

export default mount(App, { target });
