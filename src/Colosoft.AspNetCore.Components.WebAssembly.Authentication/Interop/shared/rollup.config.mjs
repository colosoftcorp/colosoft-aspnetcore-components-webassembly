import path from 'path';
import typescript from '@rollup/plugin-typescript';
import terser from '@rollup/plugin-terser';
import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import replace from '@rollup/plugin-replace';
import filesize from 'rollup-plugin-filesize';
import { env } from 'process';

export default function createBaseConfig({ inputOutputMap, dir, updateConfig }) {

  const oidcClientTsEsmPath = path.join(dir, 'node_modules', 'oidc-client-ts', 'dist', 'esm', 'oidc-client-ts.js');

  return ({ environment }) => {

    /**
     * @type {import('rollup').RollupOptions}
     */
    const baseConfig = {
      output: {
        dir: path.join(dir, '/dist', environment === 'development' ? '/Debug' : '/Release'),
        format: 'iife',
        sourcemap: environment === 'development' ? true : false,
        entryFileNames: '[name].js',
      },
      plugins: [
        {
          name: 'resolve-oidc-client-ts-esm',
          resolveId(source) {
            if (source === 'oidc-client-ts' || source === 'oidc-client-ts/dist/esm/oidc-client-ts.js') {
              return oidcClientTsEsmPath;
            }

            return null;
          }
        },
        resolve({
          browser: true,
          preferBuiltins: false,
          mainFields: ['browser', 'module', 'main'],
          exportConditions: ['browser', 'import', 'default']
        }),
        commonjs({
          transformMixedEsModules: true
        }),
        typescript({
          tsconfig: path.join(dir, 'tsconfig.json')
        }),
        replace({
          'process.env.NODE_DEBUG': 'false',
          'Platform.isNode': 'false',
          preventAssignment: true
        }),
        terser({
          compress: {
            passes: 3
          },
          mangle: true,
          module: false,
          format: {
            ecma: 2020
          },
          keep_classnames: false,
          keep_fnames: false,
          toplevel: true
        }),
        // Check the ContinuousIntegrationBuild environment variable to determine if we should show the file size.
        env.ContinuousIntegrationBuild !== 'true' && environment !== 'development' && filesize({ showMinifiedSize: true, showGzippedSize: true, showBrotliSize: true })
      ],
      treeshake: 'smallest',
      logLevel: 'silent'
    };

    return Object.entries(inputOutputMap).map(([output, input]) => {
      const config = {
        ...baseConfig,
        output: {
          ...baseConfig.output,
        },
        plugins: [
          ...baseConfig.plugins
        ],
        input: { [output]: input }
      };

      updateConfig(config, environment, output, input);

      return config;
    });
  };
};
