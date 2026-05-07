const xlsx = require('xlsx');
const fs = require('fs');
const path = require('path');

const files = [
  'c:\\Users\\monte\\Desktop\\GASTRONOMIA - PELIGRO\\Inventario equipos.xlsb',
  'c:\\Users\\monte\\Desktop\\GASTRONOMIA - PELIGRO\\inventario utensilios.xlsx',
  'c:\\Users\\monte\\Desktop\\GASTRONOMIA - PELIGRO\\tipos de equipos.xlsb',
  'c:\\Users\\monte\\Desktop\\GASTRONOMIA - PELIGRO\\tipos de utensilios v.xlsx'
];

files.forEach(file => {
  console.log(`\n\n=== ANALYZING FILE: ${path.basename(file)} ===`);
  try {
    if (!fs.existsSync(file)) {
      console.log(`File not found: ${file}`);
      return;
    }
    const workbook = xlsx.readFile(file);
    const sheetName = workbook.SheetNames[0];
    const worksheet = workbook.Sheets[sheetName];
    const data = xlsx.utils.sheet_to_json(worksheet, { header: 1 });

    console.log(`Sheet Name: ${sheetName}`);
    console.log('Headers / Samples:');
    data.slice(0, 10).forEach((row, i) => {
      console.log(`${i}: ${JSON.stringify(row)}`);
    });
  } catch (err) {
    console.log(`Error reading ${file}: ${err.message}`);
  }
});
