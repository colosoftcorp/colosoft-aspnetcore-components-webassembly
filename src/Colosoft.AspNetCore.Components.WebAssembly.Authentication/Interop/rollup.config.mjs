import path from 'path';
import { fileURLToPath } from 'url';
import createBaseConfig from './shared/rollup.config.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export default createBaseConfig({
  inputOutputMap: {
    'AuthenticationService': './AuthenticationService.ts',
  },
  dir: __dirname,
  updateConfig: (config, environment, _, input) => {
    config.output.format = 'iife';
  }
});
