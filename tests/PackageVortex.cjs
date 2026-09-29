'use strict';
// Offline contract check against the separate, unmodified DS2 Vortex extension.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const [extensionPath, ...archivePaths] = process.argv.slice(2);
assert(extensionPath && archivePaths.length, 'Pass extension source and ZIP entry paths');
const source = fs.readFileSync(extensionPath, 'utf8');
const registered = [];
const moduleObject = { exports: {} };
const context = {
  module: moduleObject, exports: moduleObject.exports,
  process: { platform: 'win32', env: {} },
  require: name => {
    if (name === 'path') return path.win32;
    if (name === 'vortex-api') return { fs: {}, log: () => {}, util: {} };
    if (name === 'winapi-bindings') throw new Error('registry unavailable in package check');
    throw new Error('Unexpected extension dependency: ' + name);
  },
};
vm.runInNewContext(source, context, { filename: extensionPath });
assert.equal(moduleObject.exports.default({
  api: { sendNotification: () => {} },
  registerGame: () => {},
  registerInstaller: (...args) => registered.push(args),
}), true);
assert.equal(registered.length, 1);
const [id, , supported, install] = registered[0];
assert.equal(id, 'deadspace2-root-and-texmod');
(async () => {
  assert.equal((await supported(archivePaths, 'deadspace2')).supported, true);
  const result = await install(archivePaths);
  assert.equal(result.instructions.length, archivePaths.length);
  const actual = result.instructions.map(item => item.destination.replaceAll('\\', '/')).sort();
  assert.deepEqual(actual, archivePaths.slice().sort());
  assert(actual.includes('version.dll'));
  assert(actual.includes('DS2TextureLauncher.exe'));
  assert(actual.includes('DS2SeamlessTextures.json'));
  console.log('PASS Vortex maps all archive entries to the intended game-root paths');
})().catch(error => { console.error(error); process.exitCode = 1; });
