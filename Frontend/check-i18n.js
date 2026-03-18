const fs = require('fs');
const path = require('path');
const { sync } = require('glob');

const i18nPath = path.join(__dirname, 'src/i18n/index.ts');
const i18nContent = fs.readFileSync(i18nPath, 'utf8');

const messagesMatch = i18nContent.match(/const messages = ({[\s\S]*?});/);
if (!messagesMatch) {
    console.error('❌ Ni mogoče najti messages v index.ts');
    process.exit(1);
}
eval('var messages = ' + messagesMatch[1]);

function flatten(obj, prefix = '') {
    let result = {};
    for (let key in obj) {
        const newKey = prefix ? `${prefix}.${key}` : key;
        if (typeof obj[key] === 'object' && obj[key] !== null) {
            Object.assign(result, flatten(obj[key], newKey));
        } else {
            result[newKey] = obj[key];
        }
    }
    return result;
}

const definedKeys = new Set(Object.keys(flatten(messages.sl)));

const files = sync('src/**/*.{vue,ts,js}', { ignore: ['**/node_modules/**', '**/dist/**'] });
const usedKeys = new Set();
const tRegex = /(?:\$t|t|i18n\.t)\s*\(\s*['"]([^'"]+)['"]/g;

files.forEach(file => {
    const content = fs.readFileSync(file, 'utf8');
    let match;
    while ((match = tRegex.exec(content)) !== null) {
        usedKeys.add(match[1]);
    }
});

const missingInI18n = Array.from(usedKeys).filter(k => !definedKeys.has(k) && !k.includes('${'));

console.log('🔍 POROČILO');
console.log('--------------------------------');
if (missingInI18n.length > 0) {
    console.log(`❌ MANJKAJOČI PREVODI (${missingInI18n.length}):`);
    missingInI18n.sort().forEach(k => console.log(` - ${k}`));
} else {
    console.log(' ✅ Vsi ključi iz kode so v i18n datoteki!');
}