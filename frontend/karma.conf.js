// Configuração do Karma.
// https://karma-runner.github.io/latest/config/configuration-file.html
//
// O navegador vem do puppeteer, instalado como devDependency: a máquina de
// desenvolvimento e a integração contínua não precisam de Chrome instalado.
//
// A partir do puppeteer 25, executablePath() é assíncrono, e a configuração do
// Karma tem de ser síncrona. Resolvemos o caminho pelo @puppeteer/browsers, que
// conhece o layout de diretório de cada plataforma.
const os = require('os');
const path = require('path');
const { Browser, computeExecutablePath } = require('@puppeteer/browsers');
const { PUPPETEER_REVISIONS } = require('puppeteer');

process.env.CHROME_BIN = computeExecutablePath({
  browser: Browser.CHROME,
  buildId: PUPPETEER_REVISIONS.chrome,
  cacheDir: process.env.PUPPETEER_CACHE_DIR || path.join(os.homedir(), '.cache', 'puppeteer')
});

module.exports = function (config) {
  config.set({
    basePath: '',
    frameworks: ['jasmine', '@angular-devkit/build-angular'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-jasmine-html-reporter'),
      require('karma-coverage'),
      require('@angular-devkit/build-angular/plugins/karma')
    ],
    client: {
      jasmine: {
        // Opções do Jasmine, se necessário:
        // https://jasmine.github.io/api/edge/Configuration.html
      },
    },
    jasmineHtmlReporter: {
      suppressAll: true // remove os rastros duplicados
    },
    coverageReporter: {
      dir: require('path').join(__dirname, './coverage/ciee-curriculos'),
      subdir: '.',
      reporters: [
        { type: 'html' },
        { type: 'text-summary' }
      ]
    },
    reporters: ['progress', 'kjhtml'],
    browsers: ['ChromeHeadlessSemSandbox'],
    customLaunchers: {
      // --no-sandbox é necessário em contêiner e no WSL, onde o sandbox do
      // Chrome não tem as permissões de que precisa.
      ChromeHeadlessSemSandbox: {
        base: 'ChromeHeadless',
        flags: ['--no-sandbox', '--disable-gpu', '--disable-dev-shm-usage']
      }
    },
    restartOnFileChange: true
  });
};
